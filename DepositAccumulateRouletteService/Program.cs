using FluentdForward.OpenTelemetry.MessagePack;
using Npgsql;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.Promotion.DepositAccumulateRouletteService;
using Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

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
			fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
				"MachineName",
				() => Environment.MachineName));
		});
	});

builder.Services
	.AddMessageQueue()
	.AddNatsMessageQueue(natsConfig => natsConfig
		.ConfigQueueOptions((options, sp) =>
		{
			var config = sp.GetRequiredService<IConfiguration>();
			options.Url = config.GetConnectionString("Nats");
		})
		.AddProcessor<DepositAccumulateRouletteSettingProcessor>(
			"tgs.promotion.depositaccumulateroulette.setting",
			"tgs.promotion.depositaccumulateroulette.setting")
		.AddProcessor<RouletteCountItemsQueryProcessor>(
			"tgs.promotion.depositaccumulateroulette.roulettecountitems.query",
			"tgs.promotion.depositaccumulateroulette.roulettecountitems.query")
		.AddProcessor<RouletteProcessor>(
			"tgs.promotion.depositaccumulateroulette.roulette.play",
			"tgs.promotion.depositaccumulateroulette.roulette.play"))
	.ClearAllExchanges()
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddMemoryCache()
	.AddSingleton<IRedisConnectionManager>(sp =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		return new RedisConnectionManager(config.GetConnectionString("Redis"));
	})
	.AddDepositAccumulateRouletteCommon()
	.AddDepositAccumulateRouletteService(sp =>
	{
		var redisConnectionManager = sp.GetRequiredService<IRedisConnectionManager>();
		return redisConnectionManager.Database;
	})
	.AddDepositAccumulateRouletteWorkerClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		options.BaseUrl = config.GetValue<Uri>("DepositAccumulateRoulette:DepositAccumulateRouletteWorkerBaseUrl");
	})
	.AddPromotionClientServices((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		options.Url = config.GetValue<Uri>("DepositAccumulateRoulette:PromotionBaseUrl");
	})
	.AddPlayerServiceClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		options.BaseUrl = config.GetValue<Uri>("DepositAccumulateRoulette:PlayerBaseUrl");
	});

builder.Services
	.AddRouting(options => options.LowercaseUrls = true)
	.AddControllers()
	.Services
	.AddDateOnlyTimeOnlyStringConverters();

builder.Services
	.AddOpenTelemetry()
	.ConfigureResource(_ => ResourceBuilder.CreateDefault().AddEnvironmentVariableDetector())
	.WithTracing(tracerProviderBuilder => tracerProviderBuilder 
		.AddSource("Tgs.*")
		.AddSource("TGS.*")
		.AddAspNetCoreInstrumentation()
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
	.AddInterceptionService()
	.AddServiceUtiltiyInterceptors();

builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapControllers();
app.MapProfileViewer();
app.MapSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/healthz");

app.Run();
