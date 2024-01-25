using FluentdForward.OpenTelemetry.MessagePack;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using NATS.Client.Core;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.Promotion.DepositAccumulateRoulette.Worker;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.MessageQueue;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
	.AddJsonFile("/config/appsettings.json", true)
	.AddEnvironmentVariables();

builder.Logging
	.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		options.ParseStateValues = true;
		_ = options.AddFluentdForwardExporter(fluentOptions =>
		{
			var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
			fluentOptions.Host = nodeIp;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
			fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
				"MachineName",
				() => Environment.MachineName));
		});
	});

builder.Services
	.AddKeyedSingleton(
		"Nats",
		(sp, _) => new NatsConnection(NatsOpts.Default with
		{
			Url = builder.Configuration.GetConnectionString("Nats")!
		}))
	.AddMessageQueue()
	.AddNatsMessageQueue(natsConfig => natsConfig
		.ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats"))
		.AddProcessor<BidProcessor>(
			"tgs.promotion.depositaccumulateroulette.bid",
			"tgs.promotion.depositaccumulateroulette.bid")
		.AddProcessor<RouletteTaskProcessor>(
			$"tgs.promotion.depositaccumulateroulette.worker.{Environment.MachineName}.roulette",
			$"tgs.promotion.depositaccumulateroulette.worker.{Environment.MachineName}.roulette")
		.AddProcessor<RouletteCountItemsQueryProcessor>(
			"tgs.promotion.depositaccumulateroulette.worker.roulettecountitems",
			"tgs.promotion.depositaccumulateroulette.worker.roulettecountitems")
		.AddHandler<ClearExpiredDepositAccumulateEventCacheHandler>(
			"tgs.promotion.depositaccumulateroulette.event.expired.clear",
			"tgs.promotion.depositaccumulateroulette.event.expired.clear"))
	.ClearAllExchanges()
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddSingleton<IRedisConnectionManager>(sp =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		return new RedisConnectionManager(config.GetConnectionString("Redis")!);
	})
	.AddMemoryCache()
	.AddDepositAccumulateRouletteCommon()
	.AddDepositAccumulateRouletteWorkerCore()
	.AddDepositAccumulateRouletteWorkerCache()
	.AddDepositAccumulateRouletteRepositories(
		dbOptionsConfigure: (sp, dbOptions) =>
		{
			var config = sp.GetRequiredService<IConfiguration>();
			_ = dbOptions.UseNpgsql(config.GetConnectionString("Tgs"));
		},
		resolveRedisDatabase: sp =>
		{
			var redisConnectionManager = sp.GetRequiredService<IRedisConnectionManager>();
			return redisConnectionManager.Database;
		})
	.AddDepositAccumulateRouletteServiceClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		options.BaseUrl = config.GetValue<Uri>("DepositAccumulateRouletteWorker:DepositAccumulateRouletteServiceBaseUrl")!;
	})
	.AddPromotionClientServices((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		options.Url = config.GetValue<Uri>("DepositAccumulateRouletteWorker:PromotionBaseUrl")!;
	})
	.AddPlayerServiceClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		options.BaseUrl = config.GetValue<Uri>("DepositAccumulateRouletteWorker:PlayerBaseUrl")!;
	});

builder.Services
	.AddHangfire((_, configuration) => configuration.UseMemoryStorage())
	.AddHangfireServer();

var excludeExtensions = new[] { "html", "ico", "png", "jpg" };
builder.Services
	.AddOpenTelemetry()
	.ConfigureResource(_ => ResourceBuilder.CreateDefault().AddEnvironmentVariableDetector())
	.WithTracing(tracerProviderBuilder => tracerProviderBuilder
		.AddSource("Tgs.*")
		.AddSource("TGS.*")
		.AddSource("Valhalla.*")
		.AddAspNetCoreInstrumentation(options => options.Filter =
			httpContext => !httpContext.Request.Path.HasValue
			   || !httpContext.Request.Path.StartsWithSegments("/swagger")
			   && !httpContext.Request.Path.StartsWithSegments("/healthz")
			   && !httpContext.Request.Path.StartsWithSegments("/profiler/")
			   && !httpContext.Request.Path.StartsWithSegments("/metrics")
			   && !excludeExtensions.Any(
				   ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
		.AddHttpClientInstrumentation()
		.AddNpgsql()
		.AddProfileViewExporter()
		.AddOtlpExporter())
	.WithMetrics(meterProviderBuilder => meterProviderBuilder
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddRuntimeInstrumentation()
		.AddOtlpExporter())
	.Services
	.AddProfileViewer();

builder.Services
	.AddRouting(options => options.LowercaseUrls = true)
	.AddControllers();

builder.Services.AddSwaggerGen();

builder.Services
	.AddInterceptionService()
	.AddServiceUtiltiyInterceptors();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapControllers();
app.MapProfileViewer();
app.MapSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/healthz");

app.MapHangfireDashboard();

app.Run();
