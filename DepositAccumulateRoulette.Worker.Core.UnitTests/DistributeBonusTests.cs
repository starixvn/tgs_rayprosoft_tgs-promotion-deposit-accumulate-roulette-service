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

		var sut = new DistributeBonusCommandService(
			fakePromotionServiceClient,
			fakeGetDistributeService);

		var command = new DistributeBonusCommand(
			Guid.NewGuid(),
			"player1",
			123456,
			100M,
			100M,
			Guid.NewGuid());

		var planId = Guid.NewGuid();
		_ = fakeGetDistributeService
			.GetAsync(Arg.Is<GetPromotionDistributePlanQuery>(x =>
				x.PromotionId == command.PromotionId && x.ConditionHash == command.DistributePlanHash))
			.Returns(new PromotionDistributePlanInfo(
				planId,
				Guid.NewGuid(),
				command.PromotionId,
				command.DistributePlanHash));

		await sut.ExecuteAsync(command);

		_ = fakePromotionServiceClient.Received(1)
			.DistributePromotionBonusAsync(
				Arg.Is(planId),
				Arg.Is(command.PlayerCode),
				Arg.Is(command.BonusAmount),
				Arg.Is(command.BucketAmount),
				Arg.Any<string>(),
				Arg.Is(command.RouletteRecordId));
	}

	[Test]
	public async Task Enqueue_Distribute_Bonus_Job()
	{
		var fakeBackgroundJobClient = Substitute.For<IBackgroundJobClient>();

		var command = new EnqueueDistributeBonusJobCommand(
			Guid.NewGuid(),
			1000);

		var scheduleInfo = new DistributeBonusScheduleInfo(
			command.RouletteRecordId,
			command.TotalDistributeAmount
		);

		var sut = new EnqueueDistributeBonusJobCommandService(
			fakeBackgroundJobClient);

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

	[Test]
	public async Task Execute_Distribute_Job()
	{
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeDistributePlanHasher = Substitute.For<IDistributePlanHasher>();
		var fakeDistributeBonusCommandService = Substitute.For<ICommandService<DistributeBonusCommand>>();
		var fakeLogger = NullLogger<DistributeBonusBackgroundJob>.Instance;

		var scheduleInfo = new DistributeBonusScheduleInfo(
			Guid.NewGuid(),
			1000);

		var rouletteRecordInfo = new RouletteRecordInfo(
			scheduleInfo.RouletteRecordId,
			"player1",
			Guid.NewGuid(),
			Guid.NewGuid(),
			DateOnly.Parse("2022-11-16"),
			2,
			30,
			90,
			1,
			DateTime.Parse("2022-11-16 08:00:00+08:00"));

		var distributePlanHash = -89153043;

		_ = fakeTgsRepository.GetRouletteRecordInfoAsync(
				Arg.Is(scheduleInfo.RouletteRecordId),
				Arg.Any<CancellationToken>())
			.Returns(rouletteRecordInfo);

		_ = fakeDistributePlanHasher.GetHashCode(
				Arg.Is(rouletteRecordInfo.EventDate),
				Arg.Is(rouletteRecordInfo.RouletteCount))
			.Returns(distributePlanHash);

		var sut = new DistributeBonusBackgroundJob(
			fakePromotionServiceClient,
			fakeTgsRepository,
			fakeDistributePlanHasher,
			fakeDistributeBonusCommandService,
			fakeLogger);

		await sut.ExecuteAsync(scheduleInfo);

		_ = fakePromotionServiceClient
			.Received(1)
			.UpdatePromotionUsedAmountAsync(
				Arg.Is(rouletteRecordInfo.PromotionId),
				Arg.Is(scheduleInfo.TotalDistributeAmount),
				Arg.Is(rouletteRecordInfo.RecordId));

		var expectedDistributeBonusCommand = new DistributeBonusCommand(
			rouletteRecordInfo.PromotionId,
			rouletteRecordInfo.PlayerCode,
			distributePlanHash,
			rouletteRecordInfo.BonusAmount,
			rouletteRecordInfo.BucketAmount,
			rouletteRecordInfo.RecordId);

		_ = fakeDistributeBonusCommandService
			.Received(1)
			.ExecuteAsync(
				Arg.Is(expectedDistributeBonusCommand));
	}
}
