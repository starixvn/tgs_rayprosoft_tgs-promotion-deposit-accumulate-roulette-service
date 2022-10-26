using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
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
		var fakeLogger = NullLogger<DistributeBonusCommandService>.Instance;

		var sut = new DistributeBonusCommandService(
			fakePromotionServiceClient,
			fakeGetDistributeService,
			fakeLogger);

		var command = new DistributeBonusCommand(
			Guid.NewGuid(),
			"player1",
			123456,
			100M,
			100M);

		var planId = Guid.NewGuid();
		_ = fakeGetDistributeService
			.GetAsync(Arg.Is<GetPromotionDistributePlanQuery>(x =>
				x.PromotionId == command.PromotionId && x.ConditionHash == command.Hash))
			.Returns(new PromotionDistributePlanInfo(
				planId,
				Guid.NewGuid(),
				command.PromotionId,
				command.Hash));

		await sut.ExecuteAsync(command);

		_ = fakePromotionServiceClient.Received(1)
			.DistributePromotionBonusAsync(
				Arg.Is(planId),
				Arg.Is(command.PlayerCode),
				Arg.Is(command.Bonus),
				Arg.Is(command.Bucket),
				Arg.Any<string>());
	}

	[Test]
	public async Task Enqueue_Distribute_Bonus_Job()
	{
		var fakeDistributePlanHasher = Substitute.For<IDistributePlanHasher>();
		var fakeBackgroundJobClient = Substitute.For<IBackgroundJobClient>();
		var fakeLogger = NullLogger<EnqueueDistributeBonusJobCommandService>.Instance;

		var command = new EnqueueDistributeBonusJobCommand(
			Guid.NewGuid(),
			"player1",
			DateOnly.Parse("2022-10-26"),
			2,
			new PlayRouletteResult.RouletteResult(
				Guid.NewGuid(),
				1000,
				10,
				30,
				Guid.NewGuid(),
				3));

		var hash = 1459347545;

		var scheduleInfo = new DistributeBonusScheduleInfo(
			command.PromotionId,
			command.PlayerCode,
			command.EventDate,
			command.UsedCount,
			command.PlayResult,
			hash
		);

		_ = fakeDistributePlanHasher.GetHashCode(
				Arg.Is(command.EventDate),
				Arg.Is(command.UsedCount))
			.Returns(hash);

		var sut = new EnqueueDistributeBonusJobCommandService(
			fakeDistributePlanHasher,
			fakeBackgroundJobClient,
			fakeLogger);

		await sut.ExecuteAsync(command);

		_ = fakeBackgroundJobClient
			.Received(1)
			.Create(
				Arg.Is<Job>(job
					=> job.Type == typeof(DistributeBonusBackgroundJob) &&
					   job.Method.Name == "ExecuteAsync"
					   && (DistributeBonusScheduleInfo)job.Args[0] == scheduleInfo),
				Arg.Is<EnqueuedState>(p => p.Name == "Enqueued"));
	}
}
