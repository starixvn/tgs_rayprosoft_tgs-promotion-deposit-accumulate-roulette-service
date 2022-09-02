using ExpectedObjects;
using NSubstitute;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class PromotionQueryTests
{
	[Test]
	public async Task Get_RoulettePromotion_Info()
	{
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var depositItems = new DepositAccumulateItem[]
		{
			new(Guid.Parse("4398bd5f-cbd9-4982-a145-c98ceb4c5ea7"), 1000M, 1),
			new(Guid.Parse("c6149bd1-ac94-4e83-897e-434e1ea0fffa"), 2800M, 3)
		};

		var rouletteItems = new RouletteItem[]
		{
			new(Guid.Parse("8eb0f681-fe27-478f-9c1d-bdfdad8d3dfc"), "iphone 13 pro", 3M, 0M, 0M),
			new(Guid.Parse("3b776382-a909-4c4c-b739-4d7974eebb47"), "1000PHP", 3M, 0.25M, 1000M),
			new(Guid.Parse("aadd13cf-e1ea-4a53-9426-46f44cb7dbd4"),"300PHP", 3M, 0.75M, 300M)
		};

		var settings = new DepositAccumulateRouletteSettings(depositItems, rouletteItems);

		var promotionInfo = new PromotionMainInfo
		{
			Id = Guid.NewGuid(),
			EventDuring = (DateTime.Now.AddDays(-10D), DateTime.Now),
			IsAutoSent = true,
			Name = "Test LotteryRoulette",
			PromotionLimitAmount = null,
			PromotionType = "DepositAccumulateRoulette"
		};

		var sut = new GetDepositAccumulateRoulettePromotionQueryService(fakePromotionServiceClient);

		var query = new GetDepositAccumulateRoulettePromotionQuery(promotionInfo.Id);

		_ = fakePromotionServiceClient.GetPromotionMainInfoAsync(Arg.Is(query.PromotionId))
			.Returns(promotionInfo);

		_ = fakePromotionServiceClient.GetPromotionRuleAsync<GetDepositAccumulateRoulettePromotionQueryService.PromotionRule>(Arg.Is(query.PromotionId))
			.Returns(new GetDepositAccumulateRoulettePromotionQueryService.PromotionRule
			{
				CompaignDetail = settings
			});

		var expected = new DepositAccumulateRoulettePromotion(
			promotionInfo.Id,
			promotionInfo.EventDuring,
			null,
			new DepositAccumulateRouletteSettings(
				new DepositAccumulateItem[]
				{
					new(Guid.Parse("4398bd5f-cbd9-4982-a145-c98ceb4c5ea7"), 1000M, 1),
					new(Guid.Parse("c6149bd1-ac94-4e83-897e-434e1ea0fffa"), 2800M, 3)
				},
				new RouletteItem[]
				{
					new(Guid.Parse("8eb0f681-fe27-478f-9c1d-bdfdad8d3dfc"), "iphone 13 pro", 3M, 0M, 0M),
					new(Guid.Parse("3b776382-a909-4c4c-b739-4d7974eebb47"), "1000PHP", 3M, 0.25M, 1000M),
					new(Guid.Parse("aadd13cf-e1ea-4a53-9426-46f44cb7dbd4"),"300PHP", 3M, 0.75M, 300M)
				}))
			.ToExpectedObject(ctx => ctx.IgnoreEqualsOverride());

		var actual = await sut.GetAsync(query);

		expected.ShouldEqual(actual);
	}

	[Test]
	public async Task Get_Player_Roulette_Promotions_When_Count_Is_Stored()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeStoreRepository = Substitute.For<IStoreRepository>();
		var fakeEventService = Substitute.For<IDepositAccumulateRouletteEventService>();

		var sut = new GetRouletteCountItemsQueryService(
			fakePlayerServiceClient,
			fakeEventService,
			fakeTgsRepository,
			fakeStoreRepository);

		var playerInfo = new PlayerInfo
		{
			PlayerCode = "player1",
			BrandId = Guid.NewGuid(),
			UserName = "player1"
		};

		var firstPromotionId = Guid.NewGuid();
		var secondPromotionId = Guid.NewGuid();

		var firstEventInfo = new DepositAccumulateRouletteEventInfo(
			playerInfo.PlayerCode,
			firstPromotionId,
			DateOnly.Parse("2022-09-01"),
			5,
			4800);

		var secondEventInfo = new DepositAccumulateRouletteEventInfo(
			playerInfo.PlayerCode,
			secondPromotionId,
			DateOnly.Parse("2022-09-01"),
			4,
			3600);

		var expected = new[]
		{
			new RouletteCountItemInfo(firstPromotionId, 4),
			new RouletteCountItemInfo(secondPromotionId, 2)
		};

		var query = new GetRouletteCountItemsQuery(playerInfo.PlayerCode);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(query.PlayerCode))
			.Returns(playerInfo);

		_ = fakeTgsRepository.GetAvailablePromotionIdsAsync(Arg.Is(playerInfo.PlayerCode),
				Arg.Any<CancellationToken>())
			.Returns(new[] { firstPromotionId, secondPromotionId }.ToAsyncEnumerable());

		_ = fakeEventService
			.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(firstPromotionId),
				Arg.Is(playerInfo.PlayerCode),
				Arg.Any<CancellationToken>())
			.Returns(new[] { firstEventInfo }.ToAsyncEnumerable());

		_ = fakeEventService.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(secondPromotionId),
				Arg.Is(playerInfo.PlayerCode),
				Arg.Any<CancellationToken>())
				.Returns(new[] { secondEventInfo }.ToAsyncEnumerable());

		_ = fakeStoreRepository
			.GetRouletteCountAsync(
				Arg.Is(firstEventInfo.PromotionId),
				Arg.Is(firstEventInfo.PlayerCode),
				Arg.Is(firstEventInfo.Date))
			.Returns(4);

		_ = fakeStoreRepository
			.GetRouletteCountAsync(
				Arg.Is(secondEventInfo.PromotionId),
				Arg.Is(secondEventInfo.PlayerCode),
				Arg.Is(secondEventInfo.Date))
			.Returns(2);

		var actual = await sut.QueryAsync(query).ToArrayAsync();

		CollectionAssert.AreEqual(expected, actual);
	}
}
