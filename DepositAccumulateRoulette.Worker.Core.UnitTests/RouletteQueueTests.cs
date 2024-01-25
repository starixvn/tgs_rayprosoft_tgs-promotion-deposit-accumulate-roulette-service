using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework.Legacy;
using Tgs.PlayerService.Client;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class RouletteQueueTests
{
	[Test]
	public async Task RouletteQueue_Execute_RouletteCommand()
	{
		var fakeRouletteServiceClient = Substitute.For<IDepositAccumulateRouletteServiceClient>();

		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();

		var fakeEventGetService = Substitute.For<IDepositAccumulateRouletteEventService>();

		var services = new ServiceCollection();
		_ = services
			.AddSingleton(fakeEventGetService)
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

		var serviceProvider = services.BuildServiceProvider();

		var sut = new RouletteCommandQueue(
			playerCode,
			promotionId,
			serviceProvider,
			fakePlayerServiceClient,
			fakeRouletteServiceClient,
			fakeSystemClock);

		var command = new StubRouletteCommand();

		await sut.StartProcessAsync(CancellationToken.None);
		sut.Enqueue(command);

		await command.Promise.Task;

		ClassicAssert.IsTrue(command.IsExecuted);
	}

	[Test]
	public void Start_Queue_If_Not_Found_Roulette_Promotion_Will_Throw_Exception()
	{
		var fakeRouletteServiceClient = Substitute.For<IDepositAccumulateRouletteServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var playerCode = "player1";

		var playerInfo = new PlayerInfo
		{
			PlayerCode = playerCode,
			BrandId = brandId
		};

		var services = new ServiceCollection();
		_ = services
			.AddSingleton(fakeServiceCoordinator);

		_ = fakePlayerServiceClient
			.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(playerInfo);

		_ = fakeRouletteServiceClient.GetDepositAccumulateRoulettePromotionAsync(
				Arg.Is(promotionId),
				Arg.Any<CancellationToken>())
			.Returns((DepositAccumulateRoulettePromotion?)null);

		var sut = new RouletteCommandQueue(
			playerCode,
			promotionId,
			services.BuildServiceProvider(),
			fakePlayerServiceClient,
			fakeRouletteServiceClient,
			fakeSystemClock);

		_ = Assert.CatchAsync(async () => await sut.StartProcessAsync(CancellationToken.None));
	}

	[Test]
	public void Start_Queue_If_Not_Found_PlayerInfo_Will_Throw_Exception()
	{
		var fakeRouletteServiceClient = Substitute.For<IDepositAccumulateRouletteServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var playerCode = "player1";

		var services = new ServiceCollection();
		_ = services
			.AddSingleton(fakeServiceCoordinator);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns((PlayerInfo?)null);

		var sut = new RouletteCommandQueue(
			playerCode,
			promotionId,
			services.BuildServiceProvider(),
			fakePlayerServiceClient,
			fakeRouletteServiceClient,
			fakeSystemClock);

		_ = Assert.CatchAsync(async () => await sut.StartProcessAsync(CancellationToken.None));
	}
}
