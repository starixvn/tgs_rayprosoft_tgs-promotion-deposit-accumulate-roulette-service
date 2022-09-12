using NSubstitute;
using Tgs.PlayerService.Client;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class RouletteServiceTests
{
	[Test]
	public async Task Play_Roulette()
	{
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new PlayRouletteCommandService(
			fakeShuffleService,
			fakeStatisticsService,
			fakeTgsRepository);

		var rouletteItems = new RouletteItem[]
		{
			new(Guid.Parse("8eb0f681-fe27-478f-9c1d-bdfdad8d3dfc"), "iphone 13 pro", 3M, 0M, 0M),
			new(Guid.Parse("3b776382-a909-4c4c-b739-4d7974eebb47"), "1000PHP", 3M, 0.25M, 1000M),
			new(Guid.Parse("aadd13cf-e1ea-4a53-9426-46f44cb7dbd4"),"300PHP", 3M, 0.75M, 300M)
		};

		var playerInfo = new PlayerInfo
		{
			PlayerCode = "player1",
			BrandId = Guid.NewGuid()
		};

		var rouletteEventCountInfo = new RouletteEventCountInfo(
			playerInfo.PlayerCode,
			Guid.NewGuid(),
			DateOnly.Parse("2022-08-11"),
			4,
			3);

		var command = new PlayRouletteCommand(
			playerInfo,
			rouletteEventCountInfo,
			rouletteItems);

		_ = fakeStatisticsService.IncrementTotalDistributeAmountAsync(
			Arg.Is(command.EventInfo.PromotionId),
			Arg.Any<decimal>())
			.Returns(100M);

		_ = fakeShuffleService
			.ShuffleAsync(
				Arg.Is(command.EventInfo.PromotionId),
				Arg.Any<IEnumerable<RouletteItem>>())
			.Returns(new[] { 0, 1, 2 });

		_ = fakeStatisticsService.IncreaseRouletteUsedCountAsync(
				Arg.Is(command.EventInfo.PromotionId),
				Arg.Is(command.EventInfo.PlayerCode),
				Arg.Is(command.EventInfo.Date),
				Arg.Any<CancellationToken>())
			.Returns((true, 1));

		var actual = await sut.GetAsync(command);

		Assert.IsNotNull(actual);
		Assert.AreNotEqual(-1, actual.Result!.ItemIndex);

		_ = fakeTgsRepository
			.Received(1)
			.AddRouletteRecordAsync(
				Arg.Is(command.EventInfo.PromotionId),
				Arg.Is(command.EventInfo.PlayerCode),
				Arg.Any<Guid>(),
				Arg.Is(command.EventInfo.Date),
				Arg.Any<decimal>(),
				Arg.Any<int>());

		_ = fakeStatisticsService
			.Received(1)
			.IncrementTotalDistributeAmountAsync(
				Arg.Is(command.EventInfo.PromotionId),
				Arg.Any<decimal>(),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Play_Roulette_If_Run_Out_Of_Roulette_Count_Cannot_Play_Roulette()
	{
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new PlayRouletteCommandService(
			fakeShuffleService,
			fakeStatisticsService,
			fakeTgsRepository);

		var rouletteItems = new RouletteItem[]
		{
			new(Guid.Parse("8eb0f681-fe27-478f-9c1d-bdfdad8d3dfc"), "iphone 13 pro", 3M, 0M, 0M),
			new(Guid.Parse("3b776382-a909-4c4c-b739-4d7974eebb47"), "1000PHP", 3M, 0.25M, 1000M),
			new(Guid.Parse("aadd13cf-e1ea-4a53-9426-46f44cb7dbd4"),"300PHP", 3M, 0.75M, 300M)
		};

		var playerInfo = new PlayerInfo
		{
			PlayerCode = "player1",
			BrandId = Guid.NewGuid()
		};

		var rouletteEventCountInfo = new RouletteEventCountInfo(
			playerInfo.PlayerCode,
			Guid.NewGuid(),
			DateOnly.Parse("2022-08-11"),
			4,
			0);

		var command = new PlayRouletteCommand(
			playerInfo,
			rouletteEventCountInfo,
			rouletteItems);

		_ = fakeStatisticsService.IncreaseRouletteUsedCountAsync(
				Arg.Is(command.EventInfo.PromotionId),
				Arg.Is(command.EventInfo.PlayerCode),
				Arg.Is(command.EventInfo.Date),
				Arg.Any<CancellationToken>())
			.Returns((true, 5));

		var actual = await sut.GetAsync(command);

		Assert.IsNull(actual.Result);

		_ = fakeTgsRepository
			.DidNotReceive()
			.AddRouletteRecordAsync(
				Arg.Is(command.EventInfo.PromotionId),
				Arg.Is(command.EventInfo.PlayerCode),
				Arg.Any<Guid>(),
				Arg.Is(command.EventInfo.Date),
				Arg.Any<decimal>(),
				Arg.Any<int>());
	}

	[Test]
	public async Task Get_Available_Event_In_Count_Distribute_Period()
	{
		var fakeEventService = Substitute.For<IDepositAccumulateRouletteEventService>();
		var fakeStoreRepository = Substitute.For<IStoreRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

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

		_ = fakeEventService.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(eventInfos.ToAsyncEnumerable());

		_ = fakeStoreRepository
			.GetRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfos[0].Date))
			.Returns(2);

		var expected = new RouletteEventCountInfo(
			eventInfos[0].PlayerCode,
			eventInfos[0].PromotionId,
			eventInfos[0].Date,
			eventInfos[0].RouletteCount,
			UsedCount: 2);

		var sut = new GetAvailableRouletteCountEventQueryService(
			fakeEventService,
			fakeStoreRepository);

		var query = new GetAvailableRouletteCountEventQuery(
			promotionId,
			playerCode);

		var actual = await sut.GetAsync(query);

		Assert.IsNotNull(actual);
		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Get_Available_Event_If_Roulette_Count_All_Used()
	{
		var fakeEventService = Substitute.For<IDepositAccumulateRouletteEventService>();
		var fakeStoreRepository = Substitute.For<IStoreRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var eventInfo = new DepositAccumulateRouletteEventInfo(
			playerCode,
			promotionId,
			DateOnly.Parse("2022-08-11"),
			4,
			3000);

		var expected = new RouletteEventCountInfo(
			playerCode,
			promotionId,
			eventInfo.Date,
			eventInfo.RouletteCount, eventInfo.RouletteCount);

		_ = fakeEventService.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(new[] { eventInfo }.ToAsyncEnumerable());

		_ = fakeStoreRepository
			.GetRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfo.Date))
			.Returns(eventInfo.RouletteCount);

		var sut = new GetAvailableRouletteCountEventQueryService(
			fakeEventService,
			fakeStoreRepository);

		var query = new GetAvailableRouletteCountEventQuery(
			promotionId,
			playerCode);

		var actual = await sut.GetAsync(query);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Get_Available_Event_If_Roulette_Count_Is_Not_Stored()
	{
		var fakeEventService = Substitute.For<IDepositAccumulateRouletteEventService>();
		var fakeStoreRepository = Substitute.For<IStoreRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var eventInfos = new[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-11"),
				4,
				3000)
		};

		_ = fakeEventService.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(eventInfos.ToAsyncEnumerable());

		_ = fakeStoreRepository
			.GetRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfos[0].Date))
			.Returns((int?)null);

		var sut = new GetAvailableRouletteCountEventQueryService(
			fakeEventService,
			fakeStoreRepository);

		var query = new GetAvailableRouletteCountEventQuery(
			promotionId,
			playerCode);

		var actual = await sut.GetAsync(query);

		Assert.IsNull(actual);
	}
}
