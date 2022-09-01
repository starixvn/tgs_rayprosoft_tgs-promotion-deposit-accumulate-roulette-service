using System.Collections.Immutable;
using ExpectedObjects;
using NSubstitute;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

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
	public async Task Can_Get_Player_Roulette_PromotionsAsync()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeEventCountQueryService = Substitute.For<IQueryService<GetRouletteEventCountQuery, RouletteEventCountInfo>>();

		var sut = new GetRouletteCountItemsQueryService(
			fakePlayerServiceClient,
			fakeTgsRepository,
			fakeEventCountQueryService);

		var playerInfo = new PlayerInfo
		{
			PlayerCode = "player1",
			BrandId = Guid.NewGuid(),
			UserName = "player1"
		};

		var expected = new[]
		{
			new RouletteCountItemInfo(Guid.NewGuid(), 4),
			new RouletteCountItemInfo(Guid.NewGuid(), 2)
		};

		var promotionIds = expected.Select(rp => rp.PromotionId).ToArray();

		var eventCountInfos = new[]
		{
			new RouletteEventCountInfo(
				EventInfo: new(playerInfo.PlayerCode, promotionIds[0], DateOnly.Parse("2022-09-01"), 5, 6000),
				4),
		};

		var query = new GetRouletteCountItemsQuery(playerInfo.PlayerCode);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(query.PlayerCode))
			.Returns(playerInfo);

		_ = fakeTgsRepository.GetAvailablePromotionIdsAsync(
				Arg.Is(playerInfo.BrandId),
				Arg.Is(playerInfo.PlayerCode),
				Arg.Any<CancellationToken>())
			.Returns(promotionIds.ToAsyncEnumerable());

		_ = fakeEventCountQueryService.QueryAsync(
				Arg.Is(new GetRouletteEventCountQuery(promotionIds[0], playerInfo.PlayerCode)),
				Arg.Any<CancellationToken>())
			.Returns(new[]
			{
				new RouletteEventCountInfo(
					EventInfo: new DepositAccumulateRouletteEventInfo(
						playerInfo.PlayerCode,
						promotionIds[0],
						DateOnly.Parse("2022-09-01"),
						5,
						6000),
					AvailableCount: 4),
			}.ToAsyncEnumerable());

		_ = fakeEventCountQueryService.QueryAsync(
					Arg.Is(new GetRouletteEventCountQuery(promotionIds[1], playerInfo.PlayerCode)),
					Arg.Any<CancellationToken>())
				.Returns(new[]
				{
					new RouletteEventCountInfo(
						EventInfo: new DepositAccumulateRouletteEventInfo(
							playerInfo.PlayerCode,
							promotionIds[1],
							DateOnly.Parse("2022-09-01"),
							4,
							4500),
						AvailableCount: 2),
				}.ToAsyncEnumerable());

		var actual = await sut.QueryAsync(query).ToArrayAsync();

		CollectionAssert.AreEqual(expected, actual);
	}
}
