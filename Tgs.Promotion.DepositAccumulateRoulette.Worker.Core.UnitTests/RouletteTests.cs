using NSubstitute;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class RouletteTests
{
	[Test]
	public async Task Play_Roulette()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetPromotionService = Substitute.For<
			IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>>();

		var fakeGetRouletteEventCountService = Substitute.For<
			IQueryService<GetRouletteEventCountQuery, RouletteEventCountInfo>>();

		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeDistributeService = Substitute.For<ICommandService<DistributeBonusCommand>>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetPromotionService,
			fakeGetRouletteEventCountService,
			fakeShuffleService,
			fakeStatisticsService,
			fakeDistributeService,
			fakePromotionServiceClient,
			fakePlayerServiceClient);

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

		var settings = new DepositAccumulateRouletteSettings(depositItems, rouletteItems);

		var eventInfo = new DepositAccumulateRouletteEventInfo(
			playerCode,
			promotionId,
			DateOnly.Parse("2022-08-11"),
			4,
			3000);

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
				new During(DateTime.Parse("2022-08-10"), DateTime.Parse("2022-08-12")),
				null,
				settings));

		_ = fakeStatisticsService.IncrementTotalDistributeAmountAsync(
			Arg.Is(command.PromotionId),
			Arg.Any<decimal>())
			.Returns(100M);

		_ = fakeShuffleService
			.ShuffleAsync(Arg.Is(command.PromotionId), Arg.Any<IEnumerable<RouletteItem>>())
			.Returns(new[] { 0, 1, 2 });

		_ = fakeGetRouletteEventCountService.QueryAsync(
				Arg.Is(new GetRouletteEventCountQuery(promotionId, playerCode)),
				Arg.Any<CancellationToken>())
			.Returns(new[]
			{
				new RouletteEventCountInfo(
					eventInfo.PlayerCode,
					eventInfo.PromotionId,
					eventInfo.Date,
					eventInfo.RouletteCount,
					AvailableCount: 3),
			}.ToAsyncEnumerable());

		_ = fakeStatisticsService.DecreaseRouletteCountAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Is(eventInfo.Date),
				Arg.Any<CancellationToken>())
			.Returns((true, 2));

		var actual = await sut.GetAsync(command);

		Assert.IsNotNull(actual);
		Assert.IsNotNull(actual.Id);
		Assert.AreNotEqual(-1, actual.ItemIndex);

		_ = fakeTgsRepository
			.Received(1)
			.AddRouletteRecordAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Any<Guid>(),
				Arg.Is(eventInfo.Date),
				Arg.Any<decimal>(),
				Arg.Any<int>());

		_ = fakeStatisticsService
			.Received(1)
			.IncrementTotalDistributeAmountAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<decimal>(),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Play_Roulette_In_Count_Distribute_Period()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetPromotionService = Substitute.For<
			IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>>();

		var fakeGetRouletteEventCountService = Substitute.For<
			IQueryService<GetRouletteEventCountQuery, RouletteEventCountInfo>>();

		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeDistributeService = Substitute.For<ICommandService<DistributeBonusCommand>>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetPromotionService,
			fakeGetRouletteEventCountService,
			fakeShuffleService,
			fakeStatisticsService,
			fakeDistributeService,
			fakePromotionServiceClient,
			fakePlayerServiceClient);

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

		var settings = new DepositAccumulateRouletteSettings(depositItems, rouletteItems);

		var eventInfos = new[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-11"),
				4,
				3000),
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-12"),
				1,
				1200)
		};

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
				new During(DateTime.Parse("2022-08-10"), DateTime.Parse("2022-08-12")),
				null,
				settings));

		_ = fakeStatisticsService.IncrementTotalDistributeAmountAsync(
			Arg.Is(command.PromotionId),
			Arg.Any<decimal>())
			.Returns(100M);

		_ = fakeShuffleService
			.ShuffleAsync(Arg.Is(command.PromotionId), Arg.Any<IEnumerable<RouletteItem>>())
			.Returns(new[] { 0, 1, 2 });

		_ = fakeGetRouletteEventCountService.QueryAsync(
				Arg.Is(new GetRouletteEventCountQuery(promotionId, playerCode)),
				Arg.Any<CancellationToken>())
			.Returns(new[]
			{
				new RouletteEventCountInfo(
					eventInfos[0].PlayerCode,
					eventInfos[0].PromotionId,
					eventInfos[0].Date,
					eventInfos[0].RouletteCount,
					AvailableCount: 2),
				new RouletteEventCountInfo(
					eventInfos[1].PlayerCode,
					eventInfos[1].PromotionId,
					eventInfos[1].Date,
					eventInfos[1].RouletteCount,
					AvailableCount: 1)
			}.ToAsyncEnumerable());

		_ = fakeStatisticsService.DecreaseRouletteCountAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Is(eventInfos[0].Date),
				Arg.Any<CancellationToken>())
			.Returns((true, 0));

		var actual = await sut.GetAsync(command);

		Assert.IsNotNull(actual);
		Assert.IsNotNull(actual.Id);
		Assert.AreNotEqual(-1, actual.ItemIndex);

		_ = fakeTgsRepository
			.Received(1)
			.AddRouletteRecordAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Any<Guid>(),
				Arg.Is(eventInfos[0].Date),
				Arg.Any<decimal>(),
				Arg.Any<int>());

		_ = fakeStatisticsService
			.Received(1)
			.IncrementTotalDistributeAmountAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<decimal>(),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public void Play_Roulette_If_Not_Found_Roulette_Promotion_Will_Throw_Exception()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetPromotionService = Substitute.For<
			IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>>();

		var fakeGetRouletteEventCountService = Substitute.For<
			IQueryService<GetRouletteEventCountQuery, RouletteEventCountInfo>>();

		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeDistributeService = Substitute.For<ICommandService<DistributeBonusCommand>>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetPromotionService,
			fakeGetRouletteEventCountService,
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

		_ = Assert.CatchAsync(async () => await sut.GetAsync(command));
	}

	[Test]
	public void Play_Roulette_If_Not_Found_PlayerInfo_Will_Throw_Exception()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetPromotionService = Substitute.For<
			IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>>();
		var fakeGetRouletteEventCountService = Substitute.For<
			IQueryService<GetRouletteEventCountQuery, RouletteEventCountInfo>>();

		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeDistributeService = Substitute.For<ICommandService<DistributeBonusCommand>>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetPromotionService,
			fakeGetRouletteEventCountService,
			fakeShuffleService,
			fakeStatisticsService,
			fakeDistributeService,
			fakePromotionServiceClient,
			fakePlayerServiceClient);

		var command = new RouletteCommand(
			Guid.NewGuid(),
			"player1");

		_ = Assert.CatchAsync(async () => await sut.GetAsync(command));
	}

	[Test]
	public async Task Play_Roulette_If_Run_Out_Of_Roulette_Count_Cannot_Play_Roulette()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetPromotionService = Substitute.For<
			IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>>();
		var fakeGetRouletteEventCountService = Substitute.For<
			IQueryService<GetRouletteEventCountQuery, RouletteEventCountInfo>>();
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeDistributeService = Substitute.For<ICommandService<DistributeBonusCommand>>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetPromotionService,
			fakeGetRouletteEventCountService,
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
			playerCode,
			promotionId,
			DateOnly.Parse("2022-08-11"),
			4,
			2000);

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
				new During(DateTime.Parse("2022-08-10"), DateTime.Parse("2022-08-12")),
				null,
				settings));

		_ = fakeStatisticsService.IncrementTotalDistributeAmountAsync(
			Arg.Is(command.PromotionId),
			Arg.Any<decimal>())
			.Returns(100M);

		_ = fakeShuffleService
			.ShuffleAsync(Arg.Is(command.PromotionId), Arg.Any<IEnumerable<RouletteItem>>())
			.Returns(new[] { 0, 1, 2 });

		_ = fakeStatisticsService.DecreaseRouletteCountAsync(
			Arg.Is(command.PromotionId),
			Arg.Is(command.PlayerCode),
			Arg.Is(eventInfo.Date),
			Arg.Any<CancellationToken>())
			.Returns((true, 0));

		var actual = await sut.GetAsync(command);

		Assert.IsNotNull(actual);
		Assert.IsNull(actual.Id);
		Assert.AreEqual(-1, actual.ItemIndex);

		_ = fakeTgsRepository
			.Received(0)
			.AddRouletteRecordAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Any<Guid>(),
				Arg.Any<DateOnly>(),
				Arg.Any<decimal>(),
				Arg.Any<int>());

		_ = fakeStatisticsService
			.Received(0)
			.IncrementTotalDistributeAmountAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<decimal>(),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Play_Roulette_Set_Current_Count_To_Distribution_Plan()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeGetPromotionService = Substitute.For<
			IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>>();
		var fakeGetRouletteEventCountService = Substitute.For<
			IQueryService<GetRouletteEventCountQuery, RouletteEventCountInfo>>();
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeDistributeService = Substitute.For<ICommandService<DistributeBonusCommand>>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var sut = new RouletteCommandService(
			fakeTgsRepository,
			fakeGetPromotionService,
			fakeGetRouletteEventCountService,
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
			playerCode,
			promotionId,
			DateOnly.Parse("2022-08-11"),
			4,
			2000);

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
				new During(DateTime.Parse("2022-08-10"), DateTime.Parse("2022-08-12")),
				null,
				settings));

		_ = fakeStatisticsService.IncrementTotalDistributeAmountAsync(
			Arg.Is(command.PromotionId),
			Arg.Any<decimal>())
			.Returns(100M);

		_ = fakeShuffleService
			.ShuffleAsync(Arg.Is(command.PromotionId), Arg.Any<IEnumerable<RouletteItem>>())
			.Returns(new[] { 0, 1, 2 });

		_ = fakeGetRouletteEventCountService.QueryAsync(
				Arg.Is(new GetRouletteEventCountQuery(promotionId, playerCode)),
				Arg.Any<CancellationToken>())
			.Returns(new[]
			{
				new RouletteEventCountInfo(
					eventInfo.PlayerCode,
					eventInfo.PromotionId,
					eventInfo.Date,
					eventInfo.RouletteCount,
					AvailableCount: 3),
			}.ToAsyncEnumerable());

		_ = fakeStatisticsService.DecreaseRouletteCountAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.PlayerCode),
				Arg.Is(eventInfo.Date),
				Arg.Any<CancellationToken>())
			.Returns((true, 2));

		var actual = await sut.GetAsync(command);

		Assert.IsNotNull(actual);
		Assert.IsNotNull(actual.Id);
		Assert.AreNotEqual(-1, actual.ItemIndex);

		_ = fakeDistributeService
			.Received(1)
			.ExecuteAsync(
				Arg.Is<DistributeBonusCommand>(x =>
					x.PromotionId == command.PromotionId &&
					x.PlayerCode == command.PlayerCode &&
					x.EventDate == eventInfo.Date &&
					x.CurrentRouletteCount == 2),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Get_Roulette_Event_Count_Infos_When_Count_Hit_Cache()
	{
		var fakeEventService = Substitute.For<IDepositAccumulateRouletteEventService>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var eventInfo = new DepositAccumulateRouletteEventInfo(
			playerCode,
			promotionId,
			DateOnly.Parse("2022-09-01"),
			6,
			6600);

		var expected = new[]
		{
			new RouletteEventCountInfo(
				eventInfo.PlayerCode,
				eventInfo.PromotionId,
				eventInfo.Date,
				eventInfo.RouletteCount,
				4)
		};

		_ = fakeEventService.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(new[] { eventInfo }.ToAsyncEnumerable());

		_ = fakeRedisRepository.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfo.Date))
			.Returns(4);

		var query = new GetRouletteEventCountQuery(promotionId, playerCode);

		var sut = new GetRouletteEventCountQueryService(
			fakeEventService,
			fakeRedisRepository,
			fakeTgsRepository);

		var actual = await sut.QueryAsync(query).ToArrayAsync();

		CollectionAssert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Get_Roulette_Event_Count_Infos_When_Count_Miss_Cache()
	{
		var fakeEventService = Substitute.For<IDepositAccumulateRouletteEventService>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var eventInfo = new DepositAccumulateRouletteEventInfo(
			playerCode,
			promotionId,
			DateOnly.Parse("2022-09-01"),
			6,
			6600);

		var expected = new[]
		{
			new RouletteEventCountInfo(
				eventInfo.PlayerCode,
				eventInfo.PromotionId,
				eventInfo.Date,
				eventInfo.RouletteCount,
				4)
		};

		_ = fakeEventService.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(new[] { eventInfo }.ToAsyncEnumerable());

		_ = fakeRedisRepository.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfo.Date))
			.Returns((int?)null);

		_ = fakeTgsRepository.GetRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfo.Date),
				Arg.Any<CancellationToken>())
			.Returns(2);

		var query = new GetRouletteEventCountQuery(promotionId, playerCode);

		var sut = new GetRouletteEventCountQueryService(
			fakeEventService,
			fakeRedisRepository,
			fakeTgsRepository);

		var actual = await sut.QueryAsync(query).ToArrayAsync();

		CollectionAssert.AreEqual(expected, actual);

		_ = fakeTgsRepository
			.Received(1)
			.GetRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfo.Date),
				Arg.Any<CancellationToken>());

		_ = fakeRedisRepository
			.Received(1)
			.SetRouletteCountAsync(
			Arg.Is(promotionId),
			Arg.Is(playerCode),
			Arg.Is(eventInfo.Date),
			Arg.Is(expected[0].AvailableCount));
	}
}
