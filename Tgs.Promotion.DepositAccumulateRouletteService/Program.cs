using Microsoft.Extensions.Options;
using Npgsql;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
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
			.AddProcessor<DepositAccumulateRouletteSettingProcessor>(
				"tgs.promotion.depositaccumulateroulette.setting",
				"tgs.promotion.depositaccumulateroulette.setting")
			.AddProcessor<RouletteCountItemsQueryProcessor>(
				"tgs.promotion.depositaccumulateroulette.roulettecountitems.query",
				"tgs.promotion.depositaccumulateroulette.roulettecountitems.query")
			.AddProcessor<RouletteProcessor>(
				"tgs.promotion.depositaccumulateroulette.roulette.play",
				"tgs.promotion.depositaccumulateroulette.roulette.play");
	})
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
	.AddInterceptionService()
	.AddServiceUtiltiyInterceptors();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.Run();
