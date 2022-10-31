using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
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
			var nodeIp = builder.Configuration.GetValue<string>("NODE_IP");
			fluentOptions.Host = nodeIp;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME");
			fluentOptions.UseMessagePack();
		});
	});

builder.Services
	.AddMessageQueue()
	.AddNatsMessageQueue(natsConfig =>
	{
		natsConfig.ConfigQueueOptions((options, sp) =>
			{
				var config = sp.GetRequiredService<IConfiguration>();
				options.Url = config.GetConnectionString("Nats");
			})
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
				"tgs.promotion.depositaccumulateroulette.event.expired.clear");
	})
	.ClearAllExchanges()
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddSingleton<IRedisConnectionManager>(sp =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		return new RedisConnectionManager(config.GetConnectionString("Redis"));
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
		options.BaseUrl = config.GetValue<Uri>("DepositAccumulateRouletteWorker:DepositAccumulateRouletteServiceBaseUrl");
	})
	.AddPromotionClientServices((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		options.Url = config.GetValue<Uri>("DepositAccumulateRouletteWorker:PromotionBaseUrl");
	})
	.AddPlayerServiceClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		options.BaseUrl = config.GetValue<Uri>("DepositAccumulateRouletteWorker:PlayerBaseUrl");
	});

builder.Services
	.AddHangfire((_, configuration) => configuration.UseMemoryStorage())
	.AddHangfireServer();

builder.Services
	.AddSingleton<IConfigureOptions<OtlpExporterOptions>>(
		sp => new ConfigureNamedOptions<OtlpExporterOptions>(
			Options.DefaultName,
			exporterOptions =>
			{
				var config = sp.GetRequiredService<IConfiguration>();
				var nodeIp = config.GetValue<string>("NODE_IP");

				if (!string.IsNullOrWhiteSpace(nodeIp))
					exporterOptions.Endpoint = new Uri($"http://{nodeIp}:4317");

				config.GetSection("Opentelemetry:Tracing").Bind(exporterOptions);
			}))
	.AddOpenTelemetryTracing(traceBuilder => traceBuilder
		.SetResourceBuilder(ResourceBuilder.CreateDefault().AddEnvironmentVariableDetector())
		.AddSource("Tgs.*")
		.AddSource("TGS.*")
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddNpgsql()
		.AddProfileViewExporter()
		.AddOtlpExporter())
	.AddProfileViewer()
	.Configure<AspNetCoreInstrumentationOptions>(
		options => options.Filter = httpContext
			=> !httpContext.Request.Path.HasValue
			   || !httpContext.Request.Path.StartsWithSegments("/swagger")
			   && !httpContext.Request.Path.StartsWithSegments("/healthz")
			   && !httpContext.Request.Path.StartsWithSegments("/profiler/")
			   && !httpContext.Request.Path.StartsWithSegments("/metrics")
			   && !new[] { "html", "ico", "png", "jpg" }.Any(
				   ext => httpContext.Request.Path.Value.EndsWith($".{ext}")));

builder.Services
	.AddRouting(options => options.LowercaseUrls = true)
	.AddControllers()
	.AddMvcOptions(mvcOptions =>
	{
		mvcOptions
			.UseDateOnlyTimeOnlyStringConverters();
	})
	.AddJsonOptions(jsonOptions => jsonOptions.UseDateOnlyTimeOnlyStringConverters());

builder.Services.AddSwaggerGen(c =>
{
	c.UseDateOnlyTimeOnlyStringConverters();
});

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
