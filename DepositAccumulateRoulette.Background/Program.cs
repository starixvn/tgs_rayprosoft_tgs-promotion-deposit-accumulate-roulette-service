using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
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
			var nodeIp = builder.Configuration.GetValue<string>("NODE_IP");
			fluentOptions.Host = nodeIp;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME");
			fluentOptions.UseMessagePack();
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
			options.Url = config.GetConnectionString("Nats");
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
		.AddProfileViewExporter()
		.AddOtlpExporter())
	.AddProfileViewer()
	.Configure<AspNetCoreInstrumentationOptions>(options => options.Filter =
		httpContext => !httpContext.Request.Path.HasValue
			|| !httpContext.Request.Path.StartsWithSegments("/swagger")
			&& !httpContext.Request.Path.StartsWithSegments("/healthz")
			&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
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
