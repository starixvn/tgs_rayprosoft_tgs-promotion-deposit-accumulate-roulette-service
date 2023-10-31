using FluentdForward.OpenTelemetry.MessagePack;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.Promotion.DepositAccumulateRoulette.Background.MessageQueue;

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
	.AddRouting(options => options.LowercaseUrls = true)
	.AddControllers()
	.Services
	.AddDateOnlyTimeOnlyStringConverters();

builder.Services
	.AddMessageQueue()
	.AddNatsMessageQueue(natsConfig => natsConfig
		.ConfigQueueOptions((options, sp) =>
		{
			var config = sp.GetRequiredService<IConfiguration>();
			options.Url = config.GetConnectionString("Nats")!;
		})
		.AddReplyHandler<DistributeDepositAccumulateRouletteCountHandler>(
			"tgs.promotion.depositaccumulateroulette.background.roulettecount.distribute",
			"tgs.promotion.depositaccumulateroulette.background.roulettecount.distribute"))
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddDepositAccumulateRouletteCommon()
	.AddDepositAccumulateRouletteBackgroundCore()
	.AddDepositAccumulateRouletteRepositories((sp, dbOptions) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		_ = dbOptions.UseNpgsql(config.GetConnectionString("Tgs"));
	})
	.AddPromotionClientServices((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		options.Url = config.GetValue<Uri>("DepositAccumulateRouletteBackground:PromotionBaseUrl")!;
	});

builder.Services
	.AddHangfire((_, configuration) => configuration.UseMemoryStorage())
	.AddHangfireServer();

builder.Services
	.AddOpenTelemetry()
	.ConfigureResource(_ => ResourceBuilder.CreateDefault().AddEnvironmentVariableDetector())
	.WithTracing(tracerProviderBuilder => tracerProviderBuilder
		.AddSource("Tgs.*")
		.AddSource("TGS.*")
		.AddSource("Valhalla.*")
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
	.Configure<AspNetCoreInstrumentationOptions>(options => options.Filter =
		httpContext => !httpContext.Request.Path.HasValue
			|| !httpContext.Request.Path.StartsWithSegments("/swagger")
			&& !httpContext.Request.Path.StartsWithSegments("/healthz")
			&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
			&& !httpContext.Request.Path.StartsWithSegments("/metrics")
			&& !new[] { "html", "ico", "png", "jpg" }.Any(
				ext => httpContext.Request.Path.Value.EndsWith($".{ext}")));

builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "DepositAccumulateRoulette.BackgroundService",
		Version = "v1"
	});

	c.ApplyWebApiToolkitSupport();
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DepositAccumulateRoulette.BackgroundService v1"));

app.MapControllers();
app.MapSwagger();
app.MapProfileViewer();
app.MapHealthChecks("/healthz");

app.MapHangfireDashboard();

app.Run();
