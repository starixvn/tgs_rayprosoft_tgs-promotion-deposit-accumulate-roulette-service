using ExpectedObjects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRouletteService;
using Tgs.Promotion.DepositAccumulateRouletteService.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core.UnitTests;

internal class PromotionTests
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
			.Returns(new GetDepositAccumulateRoulettePromotionQueryService.PromotionRule(settings));

		var expected = new DepositAccumulateRoulettePromotionInfo(
			promotionInfo.Id,
			new During(promotionInfo.EventDuring.From, promotionInfo.EventDuring.To),
			promotionInfo.PromotionLimitAmount,
			promotionInfo.IsAutoSent,
			settings)
			.ToExpectedObject(ctx => ctx.IgnoreEqualsOverride());

		var actual = await sut.GetAsync(query);

		expected.ShouldEqual(actual);
	}

	[Test]
	public async Task Get_RoulettePromotion_Info_When_Hit_Cache()
	{
		var fakeMemoryCache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions()));
		var fakeSourceService = Substitute.For<
			IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotionInfo?>>();

		var promotionId = Guid.NewGuid();

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

		var sut = new CachedGetDepositAccumulateRoulettePromotionQueryService(
			fakeMemoryCache,
			fakeSourceService);

		var query = new GetDepositAccumulateRoulettePromotionQuery(promotionId);

		var promotionInfo = new DepositAccumulateRoulettePromotionInfo(
			promotionId,
			new During(DateTime.Now.AddDays(-5), DateTime.Now.AddDays(5)),
			null,
			true,
			settings);

		_ = fakeMemoryCache.Set($"PromotionInfo:{query.PromotionId}", promotionInfo);

		var expected = promotionInfo.ToExpectedObject(
			ctx => ctx.IgnoreEqualsOverride());

		var actual = await sut.GetAsync(query);

		expected.ShouldEqual(actual);

		_ = fakeSourceService
			.DidNotReceive()
			.GetAsync(
				Arg.Is(query),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Get_RoulettePromotion_Info_When_Miss_Cache()
	{
		var fakeMemoryCache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions()));
		var fakeSourceService = Substitute.For<
			IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotionInfo?>>();

		var promotionId = Guid.NewGuid();

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

		var sut = new CachedGetDepositAccumulateRoulettePromotionQueryService(
			fakeMemoryCache,
			fakeSourceService);

		var query = new GetDepositAccumulateRoulettePromotionQuery(promotionId);

		var promotionInfo = new DepositAccumulateRoulettePromotionInfo(
			promotionId,
			new During(DateTime.Now.AddDays(-5), DateTime.Now.AddDays(5)),
			null,
			true,
			settings);

		_ = fakeSourceService.GetAsync(
				Arg.Is(query),
				Arg.Any<CancellationToken>())
			.Returns(promotionInfo);

		var expected = promotionInfo.ToExpectedObject(
			ctx => ctx.IgnoreEqualsOverride());

		var actual = await sut.GetAsync(query);

		expected.ShouldEqual(actual);

		_ = fakeSourceService
			.Received(1)
			.GetAsync(
				Arg.Is(query),
				Arg.Any<CancellationToken>());
	}
}
