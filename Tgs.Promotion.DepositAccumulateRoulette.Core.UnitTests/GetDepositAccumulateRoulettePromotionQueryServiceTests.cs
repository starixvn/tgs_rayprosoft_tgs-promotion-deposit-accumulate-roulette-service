using ExpectedObjects;
using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core.UnitTests;

internal class GetDepositAccumulateRoulettePromotionQueryServiceTests
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

		var sut = new GetDepositAccumulateRoulettePromotionQueryService(fakePromotionServiceClient);

		var query = new GetDepositAccumulateRoulettePromotionQuery(Guid.NewGuid());

		_ = fakePromotionServiceClient.GetPromotionMainInfoAsync(Arg.Is(query.PromotionId))
			.Returns(new PromotionMainInfo
			{
				Id = query.PromotionId,
				EventDuring = (DateTime.Now.AddDays(-10D), DateTime.Now),
				IsAutoSent = true,
				Name = "Test LotteryRoulette",
				PromotionLimitAmount = null,
				PromotionType = "DepositAccumulateRoulette"
			});

		_ = fakePromotionServiceClient.GetPromotionRuleAsync<GetDepositAccumulateRoulettePromotionQueryService.PromotionRule>(Arg.Is(query.PromotionId))
			.Returns(new GetDepositAccumulateRoulettePromotionQueryService.PromotionRule
			{
				CompaignDetail = settings
			});

		var expected = new DepositAccumulateRoulettePromotion(
				query.PromotionId,
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
}
