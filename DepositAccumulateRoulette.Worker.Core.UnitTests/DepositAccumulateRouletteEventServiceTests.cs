using NSubstitute;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class DepositAccumulateRouletteEventServiceTests
{
	[Test]
	public async Task Get_Data_When_Hit_Cache()
	{
		var fakeStoreRepository = Substitute.For<IStoreRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSystemClock = Substitute.For<ISystemClock>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var expected = new[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-12"),
				3,
				2200)
		};

		var queryDate = DateOnly.Parse("2022-08-12");
		var now = new DateTimeOffset(2022, 8, 12, 8, 0, 0, TimeSpan.FromHours(8D));

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeStoreRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(expected.ToAsyncEnumerable());

		var sut = new DepositAccumulateRouletteEventService(fakeSystemClock, fakeTgsRepository, fakeStoreRepository);

		var actual = await sut
			.GetDepositAccumulateRouletteEventInfosAsync(promotionId, playerCode)
			.ToArrayAsync().ConfigureAwait(false);

		CollectionAssert.AreEqual(expected, actual);

		_ = fakeTgsRepository
			.DidNotReceive()
			.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(queryDate));
	}

	[Test]
	public async Task Get_Data_When_Miss_Cache()
	{
		var fakeStoreRepository = Substitute.For<IStoreRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSystemClock = Substitute.For<ISystemClock>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var expected = new[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-12"),
				4,
				1500)
		};

		var queryDate = DateOnly.Parse("2022-08-12");
		var now = new DateTimeOffset(2022, 8, 12, 8, 0, 0, TimeSpan.FromHours(8D));
		var expireDate = new DateTimeOffset(2022, 8, 13, 8, 0, 0, TimeSpan.FromHours(8D));

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeStoreRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(Array.Empty<DepositAccumulateRouletteEventInfo>().ToAsyncEnumerable());

		_ = fakeTgsRepository
			.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(queryDate))
			.Returns(expected.ToAsyncEnumerable());

		var sut = new DepositAccumulateRouletteEventService(fakeSystemClock, fakeTgsRepository, fakeStoreRepository);

		var actual = await sut
			.GetDepositAccumulateRouletteEventInfosAsync(promotionId, playerCode)
			.ToArrayAsync().ConfigureAwait(false);

		CollectionAssert.AreEqual(expected, actual);

		_ = fakeTgsRepository
			.Received(1)
			.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(queryDate));

		_ = fakeStoreRepository
			.Received(1)
			.SetDepositAccumulateRouletteEventInfoAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(expireDate),
				Arg.Is(expected[0]));
	}

	[Test]
	public async Task Get_Data_When_Hit_Cache_And_CurrentDate_Is_Different_From_QueryDate()
	{
		var fakeStoreRepository = Substitute.For<IStoreRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSystemClock = Substitute.For<ISystemClock>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var expected = new[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-11"),
				4,
				1500),
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-12"),
				3,
				2200)
		};

		var queryDate = DateOnly.Parse("2022-08-11");
		var now = new DateTimeOffset(2022, 8, 12, 7, 20, 0, TimeSpan.FromHours(8D));

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeStoreRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(expected.ToAsyncEnumerable());

		var sut = new DepositAccumulateRouletteEventService(fakeSystemClock, fakeTgsRepository, fakeStoreRepository);

		var actual = await sut
			.GetDepositAccumulateRouletteEventInfosAsync(promotionId, playerCode)
			.ToArrayAsync().ConfigureAwait(false);

		CollectionAssert.AreEqual(expected, actual);

		_ = fakeTgsRepository
			.DidNotReceive()
			.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(queryDate));
	}

	[Test]
	public async Task Get_Data_When_Miss_Cache_And_CurrentDate_Is_Different_From_QueryDate()
	{
		var fakeStoreRepository = Substitute.For<IStoreRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSystemClock = Substitute.For<ISystemClock>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var expected = new[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-11"),
				4,
				1500),
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-12"),
				3,
				2200)
		};

		var queryDate = DateOnly.Parse("2022-08-11");
		var now = new DateTimeOffset(2022, 8, 12, 7, 20, 0, TimeSpan.FromHours(8D));

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeStoreRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(Array.Empty<DepositAccumulateRouletteEventInfo>().ToAsyncEnumerable());

		_ = fakeTgsRepository
			.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(queryDate))
			.Returns(expected.ToAsyncEnumerable());

		var sut = new DepositAccumulateRouletteEventService(fakeSystemClock, fakeTgsRepository, fakeStoreRepository);

		var actual = await sut
			.GetDepositAccumulateRouletteEventInfosAsync(promotionId, playerCode)
			.ToArrayAsync().ConfigureAwait(false);

		CollectionAssert.AreEqual(expected, actual);

		_ = fakeTgsRepository
			.Received(1)
			.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(queryDate));

		_ = fakeStoreRepository
			.Received(2)
			.SetDepositAccumulateRouletteEventInfoAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now.AddMinutes(5D)),
				Arg.Any<DepositAccumulateRouletteEventInfo>());
	}
}
