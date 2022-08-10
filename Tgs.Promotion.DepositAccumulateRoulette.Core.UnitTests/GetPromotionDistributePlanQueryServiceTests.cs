using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core.UnitTests;

internal class GetPromotionDistributePlanQueryServiceTests
{
	[Test]
	public async Task Get_Promotion_Distribute_Plan()
	{
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new GetPromotionDistributePlanQueryService(fakePromotionServiceClient);

		var query = new GetPromotionDistributePlanQuery(Guid.NewGuid(), 12345L);

		_ = await sut.GetAsync(query);

		_ = fakePromotionServiceClient.Received(1)
			.GetPromotionDistributePlanAsync(
				Arg.Is(query.PromotionId),
				Arg.Is(query.ConditionHash));
	}

	[Test]
	public async Task Get_Cached_Promotion_Distribute_Plan()
	{
		var fakeMemoryCache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions()));
		var fakePromotionDistributePlanService = Substitute.For<IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>>();

		var sut = new MemoryCachedGetPromotionDistributePlanQueryService(
			fakePromotionDistributePlanService,
			fakeMemoryCache);

		var query = new GetPromotionDistributePlanQuery(Guid.NewGuid(), 12345L);

		var expected = new PromotionDistributePlanInfo(
				Id: Guid.NewGuid(),
				BrandId: Guid.NewGuid(),
				query.PromotionId,
				query.ConditionHash);

		_ = fakeMemoryCache.Set($"{query.PromotionId}:{query.ConditionHash}", expected);

		var actual = await sut.GetAsync(query);

		Assert.AreEqual(expected, actual);

		_ = fakePromotionDistributePlanService
			.DidNotReceive()
			.GetAsync(
				Arg.Is(query),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Get_Promotion_Distribute_Plan_Miss_Cache()
	{
		var fakeMemoryCache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions()));
		var fakePromotionDistributePlanService = Substitute.For<IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>>();

		var sut = new MemoryCachedGetPromotionDistributePlanQueryService(
			fakePromotionDistributePlanService,
			fakeMemoryCache);

		var query = new GetPromotionDistributePlanQuery(Guid.NewGuid(), 12345L);

		var expected = new PromotionDistributePlanInfo(
			Id: Guid.NewGuid(),
			BrandId: Guid.NewGuid(),
			query.PromotionId,
			query.ConditionHash);

		_ = fakePromotionDistributePlanService
			.GetAsync(
				 Arg.Is(query),
				 Arg.Any<CancellationToken>())
			.Returns(expected);

		var actual = await sut.GetAsync(query);

		Assert.AreEqual(expected, actual);

		_ = fakePromotionDistributePlanService
			.Received(1)
			.GetAsync(
				Arg.Is(query),
				Arg.Any<CancellationToken>());
	}
}
