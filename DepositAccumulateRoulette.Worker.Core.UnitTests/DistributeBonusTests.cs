using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework.Internal;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class DistributeBonusTests
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
	public async Task Distribute_Bonus()
	{
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeGetDistributeService = Substitute.For<IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>>();
		var fakeDistributePlanHasher = Substitute.For<IDistributePlanHasher>();
		var fakeLogger = NullLogger<DistributeBonusCommandService>.Instance;

		var sut = new DistributeBonusCommandService(
			fakePromotionServiceClient,
			fakeGetDistributeService,
			fakeDistributePlanHasher,
			fakeLogger);

		var distributeDate = DateOnly.FromDateTime(DateTime.Now);
		var command = new DistributeBonusCommand(
			Guid.NewGuid(),
			"player1",
			distributeDate,
			3,
			100M,
			100M);

		var planId = Guid.NewGuid();
		_ = fakeGetDistributeService
			.GetAsync(Arg.Any<GetPromotionDistributePlanQuery>())
			.Returns(new PromotionDistributePlanInfo(
				planId,
				Guid.NewGuid(),
				command.PromotionId,
				distributeDate.DayNumber));

		await sut.ExecuteAsync(command);

		_ = fakePromotionServiceClient.Received(1)
			.DistributePromotionBonusAsync(
				Arg.Is(planId),
				Arg.Is(command.PlayerCode),
				Arg.Is(command.Bonus),
				Arg.Is(command.Bucket),
				Arg.Any<string>());
	}
}
