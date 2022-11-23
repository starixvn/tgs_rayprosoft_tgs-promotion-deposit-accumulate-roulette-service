using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class RouletteCommandTests
{
	[Test]
	public async Task Execute_Roulette_Command()
	{
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();
		var fakeRouletteQueueStore = Substitute.For<IRouletteQueueStore>();

		var services = new ServiceCollection();
		_ = services
			.AddSingleton(fakeServiceCoordinator)
			.AddSingleton<ILogger<RouletteCommand>>(NullLogger<RouletteCommand>.Instance);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var playerCode = "player1";

		var rouletteCountInfo = new RouletteEventCountInfo(
			playerCode,
			promotionId,
			DateOnly.Parse("2022-08-11"),
			4,
			UsedCount: 1);

		var rouletteId = Guid.NewGuid();
		var playRouletteResult = new PlayRouletteResult(
			new PlayRouletteResult.RouletteResult(
				Guid.NewGuid(),
				100M,
				10M,
				30M,
				rouletteId,
				1),
			UsedCount: 2);

		_ = fakeRouletteQueueStore.BrandId.Returns(brandId);

		_ = fakeRouletteQueueStore.GetAvailableEventAsync(Arg.Any<CancellationToken>())
			.Returns(rouletteCountInfo);

		_ = fakeServiceCoordinator.GetAsync<PlayRouletteCommand, PlayRouletteResult>(
			Arg.Is<PlayRouletteCommand>(x =>
				x.BrandId == brandId
				&& x.EventInfo == rouletteCountInfo),
			Arg.Any<CancellationToken>())
			.Returns(playRouletteResult);

		var expected = new RouletteResult(
			rouletteId,
			1);

		var sut = new RouletteCommand(
			new TaskCompletionSource<RouletteResult>());

		await sut.ExecuteAsync(
			services.BuildServiceProvider(),
			fakeRouletteQueueStore);

		var actual = await sut.Promise.Task;

		Assert.IsNotNull(actual);
		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Play_Roulette_And_Scheduled_Distribute_Info()
	{
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();
		var fakeRouletteQueueStore = Substitute.For<IRouletteQueueStore>();

		var services = new ServiceCollection();
		_ = services
			.AddSingleton(fakeServiceCoordinator)
			.AddSingleton<ILogger<RouletteCommand>>(NullLogger<RouletteCommand>.Instance);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var playerCode = "player1";

		var rouletteCountInfo = new RouletteEventCountInfo(
			playerCode,
			promotionId,
			DateOnly.Parse("2022-08-11"),
			4,
			UsedCount: 0);

		var recordId = Guid.NewGuid();

		var playRouletteResult = new PlayRouletteResult(
			new PlayRouletteResult.RouletteResult(
				recordId,
				100M,
				10M,
				30M,
				Guid.NewGuid(),
				1),
			UsedCount: 2);

		_ = fakeRouletteQueueStore.BrandId.Returns(brandId);

		_ = fakeRouletteQueueStore.GetAvailableEventAsync(Arg.Any<CancellationToken>())
			.Returns(rouletteCountInfo);

		_ = fakeServiceCoordinator.GetAsync<PlayRouletteCommand, PlayRouletteResult>(
				Arg.Is<PlayRouletteCommand>(x =>
					x.BrandId == brandId
					&& x.EventInfo == rouletteCountInfo),
				Arg.Any<CancellationToken>())
			.Returns(playRouletteResult);

		var expected = new EnqueueDistributeBonusJobCommand(
			recordId,
			100M);

		var sut = new RouletteCommand(
			new TaskCompletionSource<RouletteResult>());

		await sut.ExecuteAsync(
			services.BuildServiceProvider(),
			fakeRouletteQueueStore);

		_ = await sut.Promise.Task;

		_ = fakeServiceCoordinator
			.Received(1)
			.ExecuteAsync(
				Arg.Is(expected),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Play_Roulette_If_Run_Out_Of_Roulette_Count_Cannot_Play_Roulette()
	{
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();
		var fakeRouletteQueueStore = Substitute.For<IRouletteQueueStore>();

		var services = new ServiceCollection();
		_ = services
			.AddSingleton(fakeServiceCoordinator)
			.AddSingleton<ILogger<RouletteCommand>>(NullLogger<RouletteCommand>.Instance);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var playerCode = "player1";

		var rouletteCountInfo = new RouletteEventCountInfo(
			playerCode,
			promotionId,
			DateOnly.Parse("2022-08-11"),
			4,
			UsedCount: 3);

		var expected = new RouletteResult(null, -1);

		_ = fakeRouletteQueueStore.BrandId.Returns(brandId);

		_ = fakeRouletteQueueStore.GetAvailableEventAsync(Arg.Any<CancellationToken>())
			.Returns(rouletteCountInfo);

		_ = fakeServiceCoordinator.GetAsync<PlayRouletteCommand, PlayRouletteResult?>(
				Arg.Is<PlayRouletteCommand>(x =>
					x.BrandId == brandId
					&& x.EventInfo == rouletteCountInfo),
				Arg.Any<CancellationToken>())
			.Returns(new PlayRouletteResult(null, 0));

		var sut = new RouletteCommand(
			new TaskCompletionSource<RouletteResult>());

		await sut.ExecuteAsync(
			services.BuildServiceProvider(),
			fakeRouletteQueueStore);

		var actual = await sut.Promise.Task;

		Assert.IsNotNull(actual);
		Assert.AreEqual(expected, actual);
	}
}
