using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.DepositAccumulateRoulette.Core;
using Tgs.Promotion.DepositAccumulateRouletteService;
using Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;
using IRedisConnectionManager = Tgs.Promotion.DepositAccumulateRouletteService.IRedisConnectionManager;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
	.AddJsonFile("/config/appsettigs.json", true)
	.AddEnvironmentVariables();

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
			.AddProcessor<RouletteSettingQueryProcessor>(
				"tgs.promotion.depositaccumulateroulette.roulettesetting.query",
				"tgs.promotion.depositaccumulateroulette.roulettesetting.query")
			.AddProcessor<RouletteProcessor>(
				"tgs.promotion.depositaccumulateroulette.roulette.play",
				"tgs.promotion.depositaccumulateroulette.roulette.play")
			.SetSessionReplySubject($"tgs.promotion.depositaccumulateroulette.worker.{Environment.MachineName}.reply");
	})
	.AddNatsGlobPatternExchange("*");

builder.Services
	.AddSingleton<IRedisConnectionManager>(sp =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		return new RedisConnectionManager(config.GetConnectionString("Redis"));
	})
	.AddDepositAccumulateRouletteCommon()
	.AddMemoryCache()
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
	.AddDepositAccumulateRouletteCore(sp =>
	{
		var redisConnectionManager = sp.GetRequiredService<IRedisConnectionManager>();
		return redisConnectionManager.Database;
	})
	.AddDepositAccumulateRouletteCache()
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

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapControllers();

app.UseSwagger()
	.UseSwaggerUI();

app.MapHealthChecks("/healthz");

app.Run();
