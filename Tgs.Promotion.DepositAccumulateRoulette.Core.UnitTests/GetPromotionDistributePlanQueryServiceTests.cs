using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;

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
}
