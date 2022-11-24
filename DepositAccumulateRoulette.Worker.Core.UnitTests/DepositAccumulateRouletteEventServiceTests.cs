using System.Reflection;
using NSubstitute;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

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
			.QueryDepositAccumulateRouletteEventInfosAsync(promotionId, playerCode)
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
			.QueryDepositAccumulateRouletteEventInfosAsync(promotionId, playerCode)
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
			.QueryDepositAccumulateRouletteEventInfosAsync(promotionId, playerCode)
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
			.QueryDepositAccumulateRouletteEventInfosAsync(promotionId, playerCode)
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

	[Test]
	public async Task Get_Available_Event_In_Count_Distribute_Period()
	{
		var fakeStoreRepository = Substitute.For<IStoreRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSystemClock = Substitute.For<ISystemClock>();

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

		var now = new DateTimeOffset(2022, 8, 12, 7, 20, 0, TimeSpan.FromHours(8D));

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeStoreRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
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

		var sut = new DepositAccumulateRouletteEventService(fakeSystemClock, fakeTgsRepository, fakeStoreRepository);

		var actual = await sut
			.GetAvailableRouletteCountEventAsync(promotionId, playerCode)
			.ConfigureAwait(false);

		Assert.IsNotNull(actual);
		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Get_No_Available_Event_If_Roulette_Count_All_Used()
	{
		var fakeStoreRepository = Substitute.For<IStoreRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSystemClock = Substitute.For<ISystemClock>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var eventInfo = new DepositAccumulateRouletteEventInfo(
			playerCode,
			promotionId,
			DateOnly.Parse("2022-08-11"),
			4,
			3000);

		var now = new DateTimeOffset(2022, 8, 11, 8, 20, 0, TimeSpan.FromHours(8D));

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeStoreRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
		Arg.Is(now))
			.Returns(new[] { eventInfo }.ToAsyncEnumerable());

		_ = fakeStoreRepository
			.GetRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfo.Date))
			.Returns(eventInfo.RouletteCount);

		var sut = new DepositAccumulateRouletteEventService(fakeSystemClock, fakeTgsRepository, fakeStoreRepository);

		var actual = await sut
			.GetAvailableRouletteCountEventAsync(promotionId, playerCode)
			.ConfigureAwait(false);

		Assert.IsNull(actual);
	}

	[Test]
	public async Task Get_No_Available_Event_If_Roulette_Count_Is_Not_Stored()
	{
		var fakeStoreRepository = Substitute.For<IStoreRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSystemClock = Substitute.For<ISystemClock>();

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

		var now = new DateTimeOffset(2022, 8, 11, 8, 20, 0, TimeSpan.FromHours(8D));

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeStoreRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(now))
			.Returns(eventInfos.ToAsyncEnumerable());

		_ = fakeStoreRepository
			.GetRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfos[0].Date))
			.Returns((int?)null);

		var sut = new DepositAccumulateRouletteEventService(fakeSystemClock, fakeTgsRepository, fakeStoreRepository);

		var actual = await sut
			.GetAvailableRouletteCountEventAsync(promotionId, playerCode)
			.ConfigureAwait(false);

		Assert.IsNull(actual);
	}
}
