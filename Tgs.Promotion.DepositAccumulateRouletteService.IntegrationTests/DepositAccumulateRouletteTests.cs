using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using StackExchange.Redis;
using Tgs.MessageQueue;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Client;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;
using Tgs.Promotion.DepositAccumulateRouletteServices;
using static Tgs.Promotion.DepositAccumulateRouletteServices.ApplicationServices.GetDepositAccumulateRoulettePromotionQueryService;

namespace Tgs.Promotion.DepositAccumulateRouletteService.IntegrationTests;

internal class DepositAccumulateRouletteTests
{
	[Test]
	public async Task Can_Get_Roulette_Setting()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		await using var web = new TestDepositAccumulateRouletteServiceApplication(
			builder => builder.ConfigureServices(
				services => services
					.AddSingleton(fakePromotionServiceClient)));

		var sut = new DepositAccumulateRouletteClient(fakeMessageSender);

		var promotionId = Guid.NewGuid();

		var depositItems = new DepositAccumulateItem[]
		{
			new(Guid.Parse("4398bd5f-cbd9-4982-a145-c98ceb4c5ea7"), 1000M, 1),
			new(Guid.Parse("c6149bd1-ac94-4e83-897e-434e1ea0fffa"), 2800M, 3)
		};

		var rouletteItems = new DepositAccumulateRoulette.Common.RouletteItem[]
		{
			new(Guid.Parse("8eb0f681-fe27-478f-9c1d-bdfdad8d3dfc"), "iphone 13 pro", 3M, 0M, 0M),
			new(Guid.Parse("3b776382-a909-4c4c-b739-4d7974eebb47"), "1000PHP", 3M, 0.25M, 1000M),
			new(Guid.Parse("aadd13cf-e1ea-4a53-9426-46f44cb7dbd4"),"300PHP", 3M, 0.75M, 300M)
		};

		var settings = new DepositAccumulateRouletteSettings(depositItems, rouletteItems);

		_ = fakePromotionServiceClient.GetPromotionMainInfoAsync(Arg.Is(promotionId))
			.Returns(new PromotionMainInfo
			{
				Id = promotionId,
				EventDuring = (DateTime.UtcNow.AddDays(-10D), DateTime.UtcNow.AddDays(10D)),
				IsAutoSent = true,
				Name = "Test Deposit Accumulate Roulette",
				PromotionLimitAmount = null,
				PromotionType = "DepositAccumulateRoulette"
			});

		_ = fakePromotionServiceClient.GetPromotionRuleAsync<PromotionRule>(Arg.Is(promotionId))
			.Returns(new PromotionRule { CompaignDetail = settings });

		await using var serviceScope = web.Services.CreateAsyncScope();

		_ = fakeMessageSender.RequestAsync(
				Arg.Is("tgs.promotion.depositaccumulateroulette.roulettesetting.query"),
				Arg.Any<ReadOnlyMemory<byte>>())
			.Returns(callInfo =>
			{
				var data = callInfo.Arg<ReadOnlyMemory<byte>>();

				var processor = ActivatorUtilities.CreateInstance<RouletteSettingQueryProcessor>(serviceScope.ServiceProvider);

				return processor.HandleAsync(data).Result;
			});

		var actual = await sut.GetRouletteSettingAsync(promotionId);

		Assert.IsNotNull(actual);
		Assert.AreEqual(promotionId, actual!.PromotionId);
		Assert.AreEqual(settings.RouletteItems.Count(), actual.Items.Count());
	}

	[Test]
	public async Task Play_Roulette()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeStatisticsRepository = Substitute.For<IStatisticsRepository>();
		var fakeEventRepository = Substitute.For<IDepositAccumulateRouletteEventRepository>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeRedisConnectionManager = Substitute.For<IRedisConnectionManager>();

		await using var web = new TestDepositAccumulateRouletteServiceApplication(builder => builder
			.ConfigureServices(services => services
				.AddSingleton(fakePromotionServiceClient)
				.AddSingleton(fakeStatisticsRepository)
				.AddSingleton(fakePlayerServiceClient)
				.AddSingleton(fakeEventRepository)
				.AddSingleton(fakeRedisConnectionManager)));

		var serviceScope = web.Services.CreateAsyncScope();

		var sut = new DepositAccumulateRouletteClient(fakeMessageSender);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var playerCode = "player1";

		var depositItems = new DepositAccumulateItem[]
		{
			new(Guid.Parse("4398bd5f-cbd9-4982-a145-c98ceb4c5ea7"), 1000M, 1),
			new(Guid.Parse("c6149bd1-ac94-4e83-897e-434e1ea0fffa"), 2800M, 3)
		};

		var rouletteItems = new DepositAccumulateRoulette.Common.RouletteItem[]
		{
			new(Guid.Parse("8eb0f681-fe27-478f-9c1d-bdfdad8d3dfc"), "iphone 13 pro", 3M, 0M, 0M),
			new(Guid.Parse("3b776382-a909-4c4c-b739-4d7974eebb47"), "1000PHP", 3M, 0.25M, 1000M),
			new(Guid.Parse("aadd13cf-e1ea-4a53-9426-46f44cb7dbd4"),"300PHP", 3M, 0.75M, 300M)
		};

		var settings = new DepositAccumulateRouletteSettings(depositItems, rouletteItems);

		var eventInfo = new DepositAccumulateRouletteEventInfo(
			Guid.NewGuid(),
			playerCode,
			brandId,
			promotionId,
			DateOnly.FromDateTime(DateTime.Now),
			4,
			2000);

		_ = fakeEventRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(new[] { eventInfo }.ToAsyncEnumerable());

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId,
				UserName = "Player",
				IdentityAuthentication = true,
				IsSetPayPassword = true,
				VipLevelId = Guid.NewGuid(),
				PhoneAuthentication = true,
				EmailAuthentication = false,
				RegisteredOn = DateTime.Now.AddDays(-10)
			});

		_ = fakePromotionServiceClient.GetPromotionMainInfoAsync(Arg.Is(promotionId))
			.Returns(new PromotionMainInfo
			{
				Id = promotionId,
				EventDuring = (DateTime.UtcNow.AddDays(-10D), DateTime.UtcNow.AddDays(10D)),
				IsAutoSent = true,
				Name = "Test Deposit Accumulate Roulette",
				PromotionLimitAmount = null,
				PromotionType = "DepositAccumulateRoulette"
			});

		_ = fakePromotionServiceClient.GetPromotionRuleAsync<PromotionRule>(Arg.Is(promotionId))
			.Returns(new PromotionRule { CompaignDetail = settings });

		var key = $"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}:{eventInfo.Date:O}";
		_ = fakeRedisConnectionManager
			.Database
			.StringDecrementAsync(Arg.Is<RedisKey>(key))
			.Returns(3);

		_ = fakeMessageSender.RequestAsync(
				Arg.Is("tgs.promotion.depositaccumulateroulette.roulette.play"),
				Arg.Any<ReadOnlyMemory<byte>>())
			.Returns(callInfo =>
			{
				var data = callInfo.Arg<ReadOnlyMemory<byte>>();

				var processor = ActivatorUtilities.CreateInstance<RouletteProcessor>(serviceScope.ServiceProvider);

				return processor.HandleAsync(data).Result;
			});

		var actual = await sut.RouletteAsync(promotionId, playerCode);

		Assert.IsNotNull(actual);
		Assert.AreNotEqual(Guid.Empty, actual.Id);
		Assert.AreNotEqual(-1, actual.ItemIndex);
		Assert.AreEqual(3, actual.RemainingCount);
	}
}
