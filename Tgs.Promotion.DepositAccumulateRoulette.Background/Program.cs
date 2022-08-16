using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
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
	.AddMvcOptions(mvcOptions =>
	{
		mvcOptions
			.UseDateOnlyTimeOnlyStringConverters()
			.AddWebApiToolkitFilters();
	})
	.AddJsonOptions(jsonOptions => jsonOptions.UseDateOnlyTimeOnlyStringConverters());

builder.Services
	.AddMessageQueue()
	.AddNatsMessageQueue(natsConfig => natsConfig
		.ConfigQueueOptions((options, sp) =>
		{
			var config = sp.GetRequiredService<IConfiguration>();
			options.Url = config.GetConnectionString("Nats");
		})
		.AddReplyHandler<DepositAccumulateCalculateHandler>(
			"tgs.promotion.depositaccumulateroulette.background.calculate",
			"tgs.promotion.depositaccumulateroulette.background.calculate"))
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddDepositAccumulateRouletteCommon()
	.AddDepositAccumulateRouletteBackgroundCore()
	.AddDepositAccumulateRouletteRepositories((sp, dbOptions) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		_ = dbOptions.UseNpgsql(config.GetConnectionString("Tgs"));
	});

builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "DepositAccumulateRoulette.BackgroundService",
		Version = "v1"
	});
	c.UseDateOnlyTimeOnlyStringConverters();
	c.ApplyWebApiToolkitSupport();
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "DepositAccumulateRoulette.BackgroundService v1"));

app.MapControllers();
app.MapSwagger();
app.MapHealthChecks("/healthz");

app.Run();
