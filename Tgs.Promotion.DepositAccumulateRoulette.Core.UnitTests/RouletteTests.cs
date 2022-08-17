using NSubstitute;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRouletteServices;
using Tgs.Promotion.DepositAccumulateRouletteServices.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core.UnitTests;

internal class RouletteTests
{
	[Test]
	public async Task Play_Roulette()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetPromotionService = Substitute.For<IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>>();
		var fakeRouletteCountService = Substitute.For<IRouletteCountService>();
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeDistributeService = Substitute.For<ICommandService<DistributeBonusCommand>>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetPromotionService,
			fakeRouletteCountService,
			fakeShuffleService,
			fakeStatisticsService,
			fakeDistributeService,
			fakePromotionServiceClient,
			fakePlayerServiceClient);

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

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var playerCode = "player1";

		var settings = new DepositAccumulateRouletteSettings(depositItems, rouletteItems);

		var eventInfo = new DepositAccumulateRouletteEventInfo(
			Guid.NewGuid(),
			playerCode,
			promotionId,
			DateOnly.Parse("2022-08-11"),
			4,
			2000);

		var rouletteCountInfo = new RouletteCountInfo(eventInfo, 3, 2, 3, 2);

		var command = new RouletteCommand(
			promotionId,
			playerCode);

		_ = fakePlayerServiceClient
			.GetPlayerInfoAsync(Arg.Is(command.PlayerCode))
			.Returns(new PlayerInfo
			{
				PlayerCode = command.PlayerCode,
				BrandId = brandId
			});

		_ = fakeGetPromotionService.GetAsync(
			Arg.Is<GetDepositAccumulateRoulettePromotionQuery>(cmd =>
				cmd.PromotionId == command.PromotionId))
			.Returns(new DepositAccumulateRoulettePromotion(
				command.PromotionId,
				null,
				settings));

		_ = fakeStatisticsService.IncrementTotalDistributeAmountAsync(
			Arg.Is(command.PromotionId),
			Arg.Any<decimal>())
			.Returns(100M);

		_ = fakeShuffleService
			.ShuffleAsync(Arg.Is(command.PromotionId), Arg.Any<IEnumerable<RouletteItem>>())
			.Returns(new[] { 0, 1, 2 });

		_ = fakeRouletteCountService.DecreaseRouletteCountAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Any<CancellationToken>())
			.Returns(rouletteCountInfo);

		var actual = await sut.GetAsync(command);

		Assert.IsNotNull(actual);
		Assert.IsNotNull(actual.Id);
		Assert.AreNotEqual(-1, actual.ItemIndex);
		Assert.AreEqual(2, actual.RemainingCount);

		_ = fakeTgsRepository
			.Received(1)
			.AddRouletteRecordAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Any<Guid>(),
				Arg.Is(rouletteCountInfo.UseEvent!.EventId),
				Arg.Any<decimal>(),
				Arg.Any<int>());

		_ = fakeStatisticsService
			.Received(1)
			.IncrementTotalDistributeAmountAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<decimal>(),
				Arg.Any<CancellationToken>());

		_ = fakeRouletteCountService
			.Received(1)
			.DecreaseRouletteCountAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public void If_Not_Found_Roulette_Promotion_Will_Throw_Exception()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetPromotionService = Substitute.For<IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>>();
		var fakeRouletteCountService = Substitute.For<IRouletteCountService>();
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeDistributeService = Substitute.For<ICommandService<DistributeBonusCommand>>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetPromotionService,
			fakeRouletteCountService,
			fakeShuffleService,
			fakeStatisticsService,
			fakeDistributeService,
			fakePromotionServiceClient,
			fakePlayerServiceClient);

		var command = new RouletteCommand(
			Guid.NewGuid(),
			"player1");

		_ = fakePlayerServiceClient
			.GetPlayerInfoAsync(Arg.Is(command.PlayerCode))
			.Returns(new PlayerInfo
			{
				PlayerCode = command.PlayerCode,
				BrandId = Guid.NewGuid()
			});

		_ = Assert.ThrowsAsync<InvalidOperationException>(
			async () => await sut.GetAsync(command));
	}

	[Test]
	public void If_Not_Found_PlayerInfo_Will_Throw_Exception()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetPromotionService = Substitute.For<IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>>();
		var fakeRouletteCountService = Substitute.For<IRouletteCountService>();
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeDistributeService = Substitute.For<ICommandService<DistributeBonusCommand>>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetPromotionService,
			fakeRouletteCountService,
			fakeShuffleService,
			fakeStatisticsService,
			fakeDistributeService,
			fakePromotionServiceClient,
			fakePlayerServiceClient);

		var command = new RouletteCommand(
			Guid.NewGuid(),
			"player1");

		_ = Assert.ThrowsAsync<InvalidOperationException>(
			async () => await sut.GetAsync(command));
	}

	[Test]
	public void If_Roulette_Promotion_No_Any_Item_Will_Throw_Exception()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetPromotionService = Substitute.For<IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>>();
		var fakeRouletteCountService = Substitute.For<IRouletteCountService>();
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeDistributeService = Substitute.For<ICommandService<DistributeBonusCommand>>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetPromotionService,
			fakeRouletteCountService,
			fakeShuffleService,
			fakeStatisticsService,
			fakeDistributeService,
			fakePromotionServiceClient,
			fakePlayerServiceClient);

		var depositItems = new DepositAccumulateItem[]
		{
			new(Guid.Parse("4398bd5f-cbd9-4982-a145-c98ceb4c5ea7"), 1000M, 1),
			new(Guid.Parse("c6149bd1-ac94-4e83-897e-434e1ea0fffa"), 2800M, 3)
		};

		var settings = new DepositAccumulateRouletteSettings(depositItems, Array.Empty<RouletteItem>());

		var command = new RouletteCommand(
			Guid.NewGuid(),
			"player1");

		_ = fakeGetPromotionService.GetAsync(
				Arg.Is<GetDepositAccumulateRoulettePromotionQuery>(cmd =>
					cmd.PromotionId == command.PromotionId))
			.Returns(new DepositAccumulateRoulettePromotion(
				command.PromotionId,
				null,
				settings));

		_ = Assert.ThrowsAsync<InvalidOperationException>(async () => await sut.GetAsync(command));
	}

	[Test]
	public async Task If_Run_Out_Of_Roulette_Count_Cannot_Play_Roulette()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetPromotionService = Substitute.For<
			IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>>();
		var fakeRouletteCountService = Substitute.For<IRouletteCountService>();
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeDistributeService = Substitute.For<ICommandService<DistributeBonusCommand>>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetPromotionService,
			fakeRouletteCountService,
			fakeShuffleService,
			fakeStatisticsService,
			fakeDistributeService,
			fakePromotionServiceClient,
			fakePlayerServiceClient);

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
			Guid.NewGuid(),
			playerCode,
			promotionId,
			DateOnly.Parse("2022-08-11"),
			4,
			2000);

		var rouletteCountInfo = new RouletteCountInfo(eventInfo, 0, 0, 0, 0);

		var command = new RouletteCommand(
			Guid.NewGuid(),
			"player1");

		_ = fakePlayerServiceClient
			.GetPlayerInfoAsync(Arg.Is(command.PlayerCode))
			.Returns(new PlayerInfo
			{
				PlayerCode = command.PlayerCode,
				BrandId = brandId
			});

		_ = fakeGetPromotionService.GetAsync(
			Arg.Is<GetDepositAccumulateRoulettePromotionQuery>(cmd =>
				cmd.PromotionId == command.PromotionId))
			.Returns(new DepositAccumulateRoulettePromotion(
				command.PromotionId,
				null,
				settings));

		_ = fakeStatisticsService.IncrementTotalDistributeAmountAsync(
			Arg.Is(command.PromotionId),
			Arg.Any<decimal>())
			.Returns(100M);

		_ = fakeShuffleService
			.ShuffleAsync(Arg.Is(command.PromotionId), Arg.Any<IEnumerable<RouletteItem>>())
			.Returns(new[] { 0, 1, 2 });

		_ = fakeRouletteCountService.DecreaseRouletteCountAsync(
			Arg.Is(command.PromotionId),
			Arg.Is(command.PlayerCode),
			Arg.Any<CancellationToken>())
			.Returns(rouletteCountInfo);

		var actual = await sut.GetAsync(command);

		Assert.IsNotNull(actual);
		Assert.IsNull(actual.Id);
		Assert.AreEqual(-1, actual.ItemIndex);
		Assert.AreEqual(0, actual.RemainingCount);

		_ = fakeTgsRepository
			.Received(0)
			.AddRouletteRecordAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Any<Guid>(),
				Arg.Any<Guid>(),
				Arg.Any<decimal>(),
				Arg.Any<int>());

		_ = fakeStatisticsService
			.Received(0)
			.IncrementTotalDistributeAmountAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<decimal>(),
				Arg.Any<CancellationToken>());
	}
}
