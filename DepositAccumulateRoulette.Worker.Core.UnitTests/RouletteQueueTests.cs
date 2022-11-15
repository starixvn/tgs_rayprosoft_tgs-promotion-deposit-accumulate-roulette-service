using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class RouletteQueueTests
{
	[Test]
	public async Task Play_Roulette()
	{
		var fakeRouletteServiceClient = Substitute.For<IDepositAccumulateRouletteServiceClient>();

		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();

		var services = new ServiceCollection();
		_ = services
			.AddSingleton(fakeRouletteServiceClient)
			.AddSingleton(fakePromotionServiceClient)
			.AddSingleton(fakePlayerServiceClient)
			.AddSingleton(fakeServiceCoordinator);

		var depositItems = new DepositAccumulateItem[]
		{
			new(Guid.Parse("4398bd5f-cbd9-4982-a145-c98ceb4c5ea7"), 1000M, 1),
			new(Guid.Parse("c6149bd1-ac94-4e83-897e-434e1ea0fffa"), 2800M, 4)
		};

		var rouletteItems = new RouletteItem[]
		{
			new(Guid.Parse("8eb0f681-fe27-478f-9c1d-bdfdad8d3dfc"), "iphone 13 pro", 3M, 0M, 0M),
			new(Guid.Parse("3b776382-a909-4c4c-b739-4d7974eebb47"), "1000PHP", 3M, 0.25M, 1000M),
			new(Guid.Parse("aadd13cf-e1ea-4a53-9426-46f44cb7dbd4"),"300PHP", 3M, 0.75M, 300M)
		};

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var playerCode = "player1";

		var playerInfo = new PlayerInfo { PlayerCode = playerCode, BrandId = brandId };

		var settings = new DepositAccumulateRouletteSettings(depositItems, rouletteItems);

		var eventInfo = new DepositAccumulateRouletteEventInfo(
			playerCode,
			promotionId,
			DateOnly.Parse("2022-08-11"),
			4,
			3000);

		var rouletteCountInfo = new RouletteEventCountInfo(
			eventInfo.PlayerCode,
			eventInfo.PromotionId,
			eventInfo.Date,
			eventInfo.RouletteCount,
			UsedCount: 1);

		var playRouletteResult = new PlayRouletteResult(
			new PlayRouletteResult.RouletteResult(
				Guid.NewGuid(),
				100M,
				10M,
				30M,
				Guid.NewGuid(),
				1),
			UsedCount: 2);

		var expected = new RouletteResult(playRouletteResult.Result!.RouletteId, playRouletteResult.Result.ItemIndex);

		_ = fakePlayerServiceClient
			.GetPlayerInfoAsync(
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(playerInfo);

		_ = fakeRouletteServiceClient.GetDepositAccumulateRoulettePromotionAsync(
				Arg.Is(promotionId),
				Arg.Any<CancellationToken>())
			.Returns(new DepositAccumulateRoulettePromotion(
				promotionId,
				new EventDuring(DateTime.Parse("2022-08-10"), DateTime.Parse("2022-08-12")),
				null,
				settings));

		_ = fakeServiceCoordinator.GetAsync<GetAvailableRouletteCountEventQuery, RouletteEventCountInfo?>(
				Arg.Is(new GetAvailableRouletteCountEventQuery(promotionId, playerCode)),
				Arg.Any<CancellationToken>())
			.Returns(rouletteCountInfo);

		_ = fakeServiceCoordinator.GetAsync<PlayRouletteCommand, PlayRouletteResult?>(
			Arg.Is<PlayRouletteCommand>(x =>
				x.PlayerInfo == playerInfo
				&& x.EventInfo == rouletteCountInfo
				&& x.RouletteItems.Count() == rouletteItems.Length),
			Arg.Any<CancellationToken>())
			.Returns(playRouletteResult);

		var queue = new RouletteTaskQueue(
			playerCode,
			promotionId,
			services.BuildServiceProvider(),
			fakeSystemClock,
			NullLogger<RouletteTaskQueue>.Instance);

		queue.StartProcess(CancellationToken.None);

		var sut = new RouletteTask(new TaskCompletionSource<RouletteResult>());
		queue.Enqueue(sut);

		var actual = await sut.Promise.Task.ConfigureAwait(false);

		Assert.IsNotNull(actual);
		Assert.AreEqual(expected, actual);
	}

	[Test]
	public void Play_Roulette_If_Not_Found_Roulette_Promotion_Will_Throw_Exception()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeRouletteServiceClient = Substitute.For<IDepositAccumulateRouletteServiceClient>();

		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();

		var services = new ServiceCollection();
		_ = services
			.AddSingleton(fakeTgsRepository)
			.AddSingleton(fakeRouletteServiceClient)
			.AddSingleton(fakePromotionServiceClient)
			.AddSingleton(fakePlayerServiceClient)
			.AddSingleton(fakeServiceCoordinator);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var playerCode = "player1";

		var playerInfo = new PlayerInfo
		{
			PlayerCode = playerCode,
			BrandId = brandId
		};

		_ = fakePlayerServiceClient
			.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(playerInfo);

		_ = fakeRouletteServiceClient.GetDepositAccumulateRoulettePromotionAsync(
				Arg.Is(promotionId),
				Arg.Any<CancellationToken>())
			.Returns((DepositAccumulateRoulettePromotion?)null);

		var queue = new RouletteTaskQueue(
			playerCode,
			promotionId,
			services.BuildServiceProvider(),
			fakeSystemClock,
			NullLogger<RouletteTaskQueue>.Instance);

		queue.StartProcess(CancellationToken.None);

		var sut = new RouletteTask(new TaskCompletionSource<RouletteResult>());
		queue.Enqueue(sut);

		_ = Assert.CatchAsync(async () => await sut.Promise.Task);
	}

	[Test]
	public void Play_Roulette_If_Not_Found_PlayerInfo_Will_Throw_Exception()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeRouletteServiceClient = Substitute.For<IDepositAccumulateRouletteServiceClient>();

		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();

		var services = new ServiceCollection();
		_ = services
			.AddSingleton(fakeTgsRepository)
			.AddSingleton(fakeRouletteServiceClient)
			.AddSingleton(fakePromotionServiceClient)
			.AddSingleton(fakePlayerServiceClient)
			.AddSingleton(fakeServiceCoordinator);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var playerCode = "player1";

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns((PlayerInfo?)null);

		var queue = new RouletteTaskQueue(
			playerCode,
			promotionId,
			services.BuildServiceProvider(),
			fakeSystemClock,
			NullLogger<RouletteTaskQueue>.Instance);

		queue.StartProcess(CancellationToken.None);

		var sut = new RouletteTask(new TaskCompletionSource<RouletteResult>());
		queue.Enqueue(sut);

		_ = Assert.CatchAsync(async () => await sut.Promise.Task);
	}

	[Test]
	public async Task Play_Roulette_If_Run_Out_Of_Roulette_Count_Cannot_Play_Roulette()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeRouletteServiceClient = Substitute.For<IDepositAccumulateRouletteServiceClient>();

		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();

		var services = new ServiceCollection();
		_ = services
			.AddSingleton(fakeTgsRepository)
			.AddSingleton(fakeRouletteServiceClient)
			.AddSingleton(fakePromotionServiceClient)
			.AddSingleton(fakePlayerServiceClient)
			.AddSingleton(fakeServiceCoordinator);

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

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var playerCode = "player1";

		var eventInfo = new DepositAccumulateRouletteEventInfo(
			playerCode,
			promotionId,
			DateOnly.Parse("2022-08-11"),
			4,
			2000);

		var playerInfo = new PlayerInfo
		{
			PlayerCode = playerCode,
			BrandId = brandId
		};

		var rouletteCountInfo = new RouletteEventCountInfo(
			eventInfo.PlayerCode,
			eventInfo.PromotionId,
			eventInfo.Date,
			eventInfo.RouletteCount,
			UsedCount: 0);

		var expected = new RouletteResult(null, -1);

		_ = fakePlayerServiceClient
			.GetPlayerInfoAsync(
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(playerInfo);

		_ = fakeRouletteServiceClient.GetDepositAccumulateRoulettePromotionAsync(
				Arg.Is(promotionId),
				Arg.Any<CancellationToken>())
			.Returns(new DepositAccumulateRoulettePromotion(
				promotionId,
				new EventDuring(DateTime.Parse("2022-08-10"), DateTime.Parse("2022-08-12")),
				null,
				settings));

		_ = fakeServiceCoordinator.GetAsync<GetAvailableRouletteCountEventQuery, RouletteEventCountInfo?>(
				Arg.Is(new GetAvailableRouletteCountEventQuery(promotionId, playerCode)),
				Arg.Any<CancellationToken>())
			.Returns(rouletteCountInfo);

		_ = fakeServiceCoordinator.GetAsync<PlayRouletteCommand, PlayRouletteResult?>(
				Arg.Is<PlayRouletteCommand>(x =>
					x.PlayerInfo == playerInfo
					&& x.EventInfo == rouletteCountInfo
					&& x.RouletteItems.Count() == rouletteItems.Length),
				Arg.Any<CancellationToken>())
			.Returns(new PlayRouletteResult(null, 0));

		_ = fakeTgsRepository
			.GetRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfo.Date),
				Arg.Any<CancellationToken>())
			.Returns(3);

		var queue = new RouletteTaskQueue(
			playerCode,
			promotionId,
			services.BuildServiceProvider(),
			fakeSystemClock,
			NullLogger<RouletteTaskQueue>.Instance);

		queue.StartProcess(CancellationToken.None);

		var sut = new RouletteTask(new TaskCompletionSource<RouletteResult>());
		queue.Enqueue(sut);

		var actual = await sut.Promise.Task.ConfigureAwait(false);

		Assert.IsNotNull(actual);
		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Play_Roulette_Set_Current_Count_To_Distribution_Plan()
	{
		var fakeRouletteServiceClient = Substitute.For<IDepositAccumulateRouletteServiceClient>();

		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();

		var services = new ServiceCollection();
		_ = services
			.AddSingleton(fakeRouletteServiceClient)
			.AddSingleton(fakePromotionServiceClient)
			.AddSingleton(fakePlayerServiceClient)
			.AddSingleton(fakeServiceCoordinator);

		var depositItems = new DepositAccumulateItem[]
		{
			new(Guid.Parse("4398bd5f-cbd9-4982-a145-c98ceb4c5ea7"), 1000M, 1),
			new(Guid.Parse("c6149bd1-ac94-4e83-897e-434e1ea0fffa"), 2800M, 4)
		};

		var rouletteItems = new RouletteItem[]
		{
			new(Guid.Parse("8eb0f681-fe27-478f-9c1d-bdfdad8d3dfc"), "iphone 13 pro", 3M, 0M, 0M),
			new(Guid.Parse("3b776382-a909-4c4c-b739-4d7974eebb47"), "1000PHP", 3M, 0.25M, 1000M),
			new(Guid.Parse("aadd13cf-e1ea-4a53-9426-46f44cb7dbd4"),"300PHP", 3M, 0.75M, 300M)
		};

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var playerCode = "player1";

		var playerInfo = new PlayerInfo { PlayerCode = playerCode, BrandId = brandId };

		var settings = new DepositAccumulateRouletteSettings(depositItems, rouletteItems);

		var eventInfo = new DepositAccumulateRouletteEventInfo(
			playerCode,
			promotionId,
			DateOnly.Parse("2022-08-11"),
			4,
			3000);

		var rouletteCountInfo = new RouletteEventCountInfo(
			eventInfo.PlayerCode,
			eventInfo.PromotionId,
			eventInfo.Date,
			eventInfo.RouletteCount,
			UsedCount: 0);

		var playRouletteResult = new PlayRouletteResult(
			new PlayRouletteResult.RouletteResult(
				Guid.NewGuid(),
				100M,
				10M,
				30M,
				Guid.NewGuid(),
				1),
			UsedCount: 2);

		var expected = new RouletteResult(playRouletteResult.Result!.RouletteId, playRouletteResult.Result.ItemIndex);

		_ = fakePlayerServiceClient
			.GetPlayerInfoAsync(
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(playerInfo);

		_ = fakeRouletteServiceClient.GetDepositAccumulateRoulettePromotionAsync(
				Arg.Is(promotionId),
				Arg.Any<CancellationToken>())
			.Returns(new DepositAccumulateRoulettePromotion(
				promotionId,
				new EventDuring(DateTime.Parse("2022-08-10"), DateTime.Parse("2022-08-12")),
				null,
				settings));

		_ = fakeServiceCoordinator.GetAsync<GetAvailableRouletteCountEventQuery, RouletteEventCountInfo?>(
				Arg.Is(new GetAvailableRouletteCountEventQuery(promotionId, playerCode)),
				Arg.Any<CancellationToken>())
			.Returns(rouletteCountInfo);

		_ = fakeServiceCoordinator.GetAsync<PlayRouletteCommand, PlayRouletteResult?>(
			Arg.Is<PlayRouletteCommand>(x =>
				x.PlayerInfo == playerInfo
				&& x.EventInfo == rouletteCountInfo
				&& x.RouletteItems.Count() == rouletteItems.Length),
			Arg.Any<CancellationToken>())
			.Returns(playRouletteResult);

		var queue = new RouletteTaskQueue(
			playerCode,
			promotionId,
			services.BuildServiceProvider(),
			fakeSystemClock,
			NullLogger<RouletteTaskQueue>.Instance);

		queue.StartProcess(CancellationToken.None);

		var sut = new RouletteTask(new TaskCompletionSource<RouletteResult>());
		queue.Enqueue(sut);

		var enqueueCommand = new EnqueueDistributeBonusJobCommand(
			promotionId,
			playerCode,
			eventInfo.Date,
			2,
			playRouletteResult.Result);

		var actual = await sut.Promise.Task.ConfigureAwait(false);

		Assert.IsNotNull(actual);
		Assert.AreEqual(expected, actual);

		_ = fakeServiceCoordinator
			.Received(1)
			.ExecuteAsync(
				Arg.Is(enqueueCommand),
				Arg.Any<CancellationToken>());
	}
}
