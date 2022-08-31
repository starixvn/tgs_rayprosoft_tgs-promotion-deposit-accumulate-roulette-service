using NSubstitute;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class StatisticsServiceTests
{
	[Test]
	public async Task Increase_Total_Used_Amount()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var distributeAmount = 20m;

		var expected = 100m;

		_ = fakeRedisRepository.IncrementTotalDistributeAmountAsync(
				Arg.Is(promotionId),
				Arg.Is(distributeAmount))
			.Returns(expected);

		var sut = new StatisticsService(fakeSystemClock, fakeTgsRepository, fakeRedisRepository);

		var actual = await sut.IncrementTotalDistributeAmountAsync(promotionId, distributeAmount);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Decrease_RouletteCount_When_Roulette_Count_Hit_Redis_Cache()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var queryDate = DateOnly.Parse("2022-08-12");
		var now = new DateTimeOffset(2022, 8, 12, 8, 0, 0, TimeSpan.FromHours(8D));

		var events = new DepositAccumulateRouletteEventInfo[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-12"),
				5,
				2400)
		};

		var expected = new RouletteCountInfo(events[0], 3, 2, 3, 2);

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeRedisRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(events.ToAsyncEnumerable());

		_ = fakeRedisRepository.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(3);

		_ = fakeRedisRepository.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(2);

		var sut = new StatisticsService(fakeSystemClock, fakeTgsRepository, fakeRedisRepository);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Decrease_RouletteCount_When_Roulette_Count_Miss_Redis_Cache()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var queryDate = DateOnly.Parse("2022-08-12");
		var now = new DateTimeOffset(2022, 8, 12, 8, 0, 0, TimeSpan.FromHours(8D));

		var events = new DepositAccumulateRouletteEventInfo[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-12"),
				5,
				2400)
		};

		var expected = new RouletteCountInfo(events[0], 3, 2, 3, 2);

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeRedisRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(events.ToAsyncEnumerable());

		_ = fakeRedisRepository.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns((int?)null);

		_ = fakeTgsRepository
			.GetRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
					Arg.Is(events[0].Date))
			.Returns(2);

		_ = fakeRedisRepository
			.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(2);

		var sut = new StatisticsService(fakeSystemClock, fakeTgsRepository, fakeRedisRepository);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);

		_ = fakeRedisRepository
			.Received(1)
			.SetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date),
				Arg.Is(3));
	}

	[Test]
	public async Task Decrease_RouletteCount_When_Event_Data_Is_Empty()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var queryDate = DateOnly.Parse("2022-08-12");
		var now = new DateTimeOffset(2022, 8, 12, 8, 0, 0, TimeSpan.FromHours(8D));

		var expected = new RouletteCountInfo(null, 0, 0, 0, 0);

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeRedisRepository
			.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(Array.Empty<DepositAccumulateRouletteEventInfo>().ToAsyncEnumerable());

		var sut = new StatisticsService(fakeSystemClock, fakeTgsRepository, fakeRedisRepository);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);

		_ = fakeRedisRepository
			.Received(0)
			.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<DateOnly>());

		_ = fakeRedisRepository
			.Received(0)
			.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<DateOnly>());
	}

	[Test]
	public async Task Decrease_RouletteCount_With_Two_Events_Returns()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var queryDate = DateOnly.Parse("2022-08-10");
		var now = new DateTimeOffset(2022, 8, 11, 7, 20, 0, TimeSpan.FromHours(8D));

		var events = new[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-10"),
				5,
				2400),
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-11"),
				4,
				1200)
		};

		var expected = new RouletteCountInfo(events[0], 3, 2, 7, 6);

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeRedisRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(events.ToAsyncEnumerable());

		_ = fakeRedisRepository
			.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(3);

		_ = fakeRedisRepository
			.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[1].Date))
			.Returns(4);

		_ = fakeRedisRepository
			.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(2);

		var sut = new StatisticsService(fakeSystemClock, fakeTgsRepository, fakeRedisRepository);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Decrease_RouletteCount_With_Two_Events_Returns_And_First_Event_Has_Run_Out_Of_Roulette_Count()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var queryDate = DateOnly.Parse("2022-08-10");
		var now = new DateTimeOffset(2022, 8, 11, 7, 20, 0, TimeSpan.FromHours(8D));

		var events = new DepositAccumulateRouletteEventInfo[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-10"),
				5,
				2400),
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-11"),
				4,
				1200)
		};

		var expected = new RouletteCountInfo(events[1], 4, 3, 4, 3);

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeRedisRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(events.ToAsyncEnumerable());

		_ = fakeRedisRepository
			.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(0);

		_ = fakeRedisRepository
			.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[1].Date))
			.Returns(4);

		_ = fakeRedisRepository.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[1].Date))
			.Returns(3);

		var sut = new StatisticsService(fakeSystemClock, fakeTgsRepository, fakeRedisRepository);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Increase_RouletteCount()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var date = DateOnly.Parse("2022-08-11");

		var sut = new StatisticsService(fakeSystemClock, fakeTgsRepository, fakeRedisRepository);

		await sut.IncreaseRouletteCountAsync(promotionId, playerCode, date).ConfigureAwait(false);

		_ = fakeRedisRepository
			.Received(1)
			.IncreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(date));
	}

	[Test]
	public async Task Get_Available_Roulette_Count_When_Roulette_Count_Hit_Redis_Cache()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var queryDate = DateOnly.Parse("2022-08-10");
		var now = new DateTimeOffset(2022, 8, 11, 7, 20, 0, TimeSpan.FromHours(8D));

		var events = new DepositAccumulateRouletteEventInfo[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-10"),
				5,
				2400),
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-11"),
				4,
				1200)
		};

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeRedisRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(events.ToAsyncEnumerable());

		_ = fakeRedisRepository
			.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(0);

		_ = fakeRedisRepository
			.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[1].Date))
			.Returns(4);

		var sut = new StatisticsService(fakeSystemClock, fakeTgsRepository, fakeRedisRepository);

		var actual = await sut.GetAvailableRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(4, actual);
	}

	[Test]
	public async Task Get_Available_Roulette_Count_When_Roulette_Count_Miss_Redis_Cache()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var queryDate = DateOnly.Parse("2022-08-10");
		var now = new DateTimeOffset(2022, 8, 11, 7, 20, 0, TimeSpan.FromHours(8D));

		var events = new DepositAccumulateRouletteEventInfo[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-10"),
				5,
				2400),
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-11"),
				4,
				1200)
		};

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeRedisRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(events.ToAsyncEnumerable());

		_ = fakeRedisRepository
			.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns((int?)null);

		_ = fakeTgsRepository.GetRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(5);

		_ = fakeRedisRepository
			.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[1].Date))
			.Returns((int?)null);

		_ = fakeTgsRepository.GetRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[1].Date))
			.Returns(0);

		var sut = new StatisticsService(fakeSystemClock, fakeTgsRepository, fakeRedisRepository);

		var actual = await sut.GetAvailableRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(4, actual);

		_ = fakeRedisRepository
			.Received(2)
			.SetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<DateOnly>(),
				Arg.Any<int>());
	}

	[Test]
	public async Task Decrease_RouletteCount_When_Deposit_Events_Hit_Redis_Cache()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var queryDate = DateOnly.Parse("2022-08-12");
		var now = new DateTimeOffset(2022, 8, 12, 8, 0, 0, TimeSpan.FromHours(8D));

		var events = new DepositAccumulateRouletteEventInfo[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-12"),
				5,
				2400)
		};

		var expected = new RouletteCountInfo(events[0], 3, 2, 3, 2);

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeRedisRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(events.ToAsyncEnumerable());

		_ = fakeRedisRepository.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(3);

		_ = fakeRedisRepository.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(2);

		var sut = new StatisticsService(fakeSystemClock, fakeTgsRepository, fakeRedisRepository);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);

		_ = fakeTgsRepository
			.DidNotReceive()
			.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(queryDate));
	}

	[Test]
	public async Task Decrease_RouletteCount_When_Deposit_Events_Miss_Redis_Cache()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var queryDate = DateOnly.Parse("2022-08-12");
		var now = new DateTimeOffset(2022, 8, 12, 8, 0, 0, TimeSpan.FromHours(8D));
		var expireDate = new DateTimeOffset(2022, 8, 13, 8, 0, 0, TimeSpan.FromHours(8D));

		var events = new DepositAccumulateRouletteEventInfo[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-12"),
				5,
				2400)
		};

		var expected = new RouletteCountInfo(events[0], 3, 2, 3, 2);

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeRedisRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(Array.Empty<DepositAccumulateRouletteEventInfo>().ToAsyncEnumerable());

		_ = fakeTgsRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(queryDate))
			.Returns(events.ToAsyncEnumerable());

		_ = fakeRedisRepository.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(3);

		_ = fakeRedisRepository.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(2);

		var sut = new StatisticsService(fakeSystemClock, fakeTgsRepository, fakeRedisRepository);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);

		_ = fakeTgsRepository
			.Received(1)
			.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(queryDate));

		_ = fakeRedisRepository
			.Received(1)
			.SetDepositAccumulateRouletteEventInfoAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(expireDate),
				Arg.Is(events[0]));
	}

	[Test]
	public async Task Decrease_RouletteCount_WhenDeposit_Events_Hit_Cache_And_CurrentDate_Is_Different_From_QueryDate()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var queryDate = DateOnly.Parse("2022-08-11");
		var now = new DateTimeOffset(2022, 8, 12, 7, 20, 0, TimeSpan.FromHours(8D));

		var events = new[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-11"),
				3,
				1500),
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-12"),
				2,
				2200)
		};

		var expected = new RouletteCountInfo(events[0], 3, 2, 5, 4);

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeRedisRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(events.ToAsyncEnumerable());

		_ = fakeRedisRepository.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(3);

		_ = fakeRedisRepository.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(2);

		var sut = new StatisticsService(fakeSystemClock, fakeTgsRepository, fakeRedisRepository);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);

		_ = fakeTgsRepository
			.DidNotReceive()
			.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(queryDate));
	}

	[Test]
	public async Task Decrease_RouletteCount_WhenDeposit_Events_Miss_Cache_And_CurrentDate_Is_Different_From_QueryDate()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var queryDate = DateOnly.Parse("2022-08-11");
		var now = new DateTimeOffset(2022, 8, 12, 7, 20, 0, TimeSpan.FromHours(8D));

		var events = new[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-11"),
				3,
				1500),
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-12"),
				2,
				2200)
		};

		var expected = new RouletteCountInfo(events[0], 3, 2, 5, 4);

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeRedisRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(Array.Empty<DepositAccumulateRouletteEventInfo>().ToAsyncEnumerable());

		_ = fakeTgsRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(queryDate))
			.Returns(events.ToAsyncEnumerable());

		_ = fakeRedisRepository.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(3);

		_ = fakeRedisRepository.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(2);

		var sut = new StatisticsService(fakeSystemClock, fakeTgsRepository, fakeRedisRepository);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);

		_ = fakeTgsRepository
			.Received(1)
			.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(queryDate));
	}
}
