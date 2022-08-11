using System.Linq.Expressions;
using NSubstitute;
using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core.UnitTests;

internal class RouletteCountServiceTests
{
	[Test]
	public async Task Decrease_RouletteCount_When_Hit_Redis_Cache()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();
		var fakeEventQueryService =
			Substitute.For<IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo>>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();
		var fakeSystemClock = Substitute.For<ISystemClock>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		var events = new DepositAccumulateRouletteEventInfo[]
		{
			new DepositAccumulateRouletteEventInfo(
				Guid.NewGuid(),
				playerCode,
				brandId,
				promotionId,
				DateOnly.Parse("2022-08-10"),
				5,
				2400)
		};

		var expected = new RouletteCountInfo(events[0], 3, 2, 3, 2);

		_ = fakeSystemClock.UtcNow.Returns(new DateTimeOffset(2022, 08, 11, 6, 0, 0, TimeSpan.FromHours(8)));

		_ = fakeEventQueryService.QueryAsync(
				Arg.Is(new GetDepositAccumulateEventsQuery(
					promotionId,
					playerCode,
					DateOnly.Parse("2022-08-10"))),
				Arg.Any<CancellationToken>())
			.Returns(events.ToAsyncEnumerable());

		var countKey =
			$"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}:{events[0].Date:O}";

		_ = fakeRedisDatabase.StringGetAsync(
			Arg.Is<RedisKey>(countKey))
			.Returns(3);

		_ = fakeRedisDatabase.StringDecrementAsync(
				Arg.Is<RedisKey>(countKey))
			.Returns(2);

		var sut = new RouletteCountService(fakeRedisDatabase, fakeEventQueryService, fakeUsedCountQueryService, fakeSystemClock);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Decrease_RouletteCount_When_Miss_Redis_Cache()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();
		var fakeEventQueryService =
			Substitute.For<IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo>>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();
		var fakeSystemClock = Substitute.For<ISystemClock>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		var events = new DepositAccumulateRouletteEventInfo[]
		{
			new DepositAccumulateRouletteEventInfo(
				Guid.NewGuid(),
				playerCode,
				brandId,
				promotionId,
				DateOnly.Parse("2022-08-11"),
				5,
				2400)
		};

		var expected = new RouletteCountInfo(events[0], 3, 2, 3, 2);

		_ = fakeSystemClock.UtcNow.Returns(new DateTimeOffset(2022, 08, 11, 7, 30, 0, TimeSpan.FromHours(8)));

		_ = fakeEventQueryService.QueryAsync(
				Arg.Is(new GetDepositAccumulateEventsQuery(
					promotionId,
					playerCode,
					DateOnly.Parse("2022-08-11"))),
				Arg.Any<CancellationToken>())
			.Returns(events.ToAsyncEnumerable());

		var countKey =
			$"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}:{events[0].Date:O}";

		_ = fakeRedisDatabase.StringGetAsync(
				Arg.Is<RedisKey>(countKey))
			.Returns((int?)null);

		_ = fakeUsedCountQueryService.GetAsync(
				Arg.Is(new RouletteUsedCountQuery(
					promotionId,
					playerCode,
					events[0].EventId)))
			.Returns(2);

		_ = fakeRedisDatabase.StringSetAsync(
				Arg.Is<RedisKey>(countKey),
				Arg.Is<RedisValue>(3),
				Arg.Any<TimeSpan>())
			.Returns(true);

		_ = fakeRedisDatabase.StringDecrementAsync(
				Arg.Is<RedisKey>(countKey))
			.Returns(2);

		var sut = new RouletteCountService(fakeRedisDatabase, fakeEventQueryService, fakeUsedCountQueryService, fakeSystemClock);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Decrease_RouletteCount_When_Event_Data_Are_Empty()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();
		var fakeEventQueryService =
			Substitute.For<IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo>>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();
		var fakeSystemClock = Substitute.For<ISystemClock>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		var expected = new RouletteCountInfo(null, 0, 0, 0, 0);

		_ = fakeSystemClock.UtcNow.Returns(new DateTimeOffset(2022, 08, 11, 7, 30, 0, TimeSpan.FromHours(8)));

		_ = fakeEventQueryService.QueryAsync(
				Arg.Is(new GetDepositAccumulateEventsQuery(
					promotionId,
					playerCode,
					DateOnly.Parse("2022-08-11"))),
				Arg.Any<CancellationToken>())
			.Returns(Array.Empty<DepositAccumulateRouletteEventInfo>().ToAsyncEnumerable());

		var sut = new RouletteCountService(fakeRedisDatabase, fakeEventQueryService, fakeUsedCountQueryService, fakeSystemClock);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);

		var keyPrefix = $"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}";

		_ = fakeRedisDatabase
			.Received(0)
			.StringGetAsync(
				Arg.Is<RedisKey>(x => x.ToString().StartsWith(keyPrefix)));

		_ = fakeRedisDatabase
			.Received(0)
			.StringDecrementAsync(
				Arg.Is<RedisKey>(x => x.ToString().StartsWith(keyPrefix)));
	}

	[Test]
	public async Task Get_RouletteCount_With_Two_Events_Returns()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();
		var fakeEventQueryService =
			Substitute.For<IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo>>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();
		var fakeSystemClock = Substitute.For<ISystemClock>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		var events = new DepositAccumulateRouletteEventInfo[]
		{
			new DepositAccumulateRouletteEventInfo(
				Guid.NewGuid(),
				playerCode,
				brandId,
				promotionId,
				DateOnly.Parse("2022-08-10"),
				5,
				2400),
			new DepositAccumulateRouletteEventInfo(
				Guid.NewGuid(),
				playerCode,
				brandId,
				promotionId,
				DateOnly.Parse("2022-08-11"),
				4,
				1200)
		};

		var expected = new RouletteCountInfo(events[0], 3, 2, 7, 6);

		_ = fakeSystemClock.UtcNow.Returns(new DateTimeOffset(2022, 08, 11, 6, 0, 0, TimeSpan.FromHours(8)));

		_ = fakeEventQueryService.QueryAsync(
				Arg.Is(new GetDepositAccumulateEventsQuery(
					promotionId,
					playerCode,
					DateOnly.Parse("2022-08-10"))),
				Arg.Any<CancellationToken>())
			.Returns(events.ToAsyncEnumerable());

		var countKeyPrefix =
			$"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}";

		_ = fakeRedisDatabase.StringGetAsync(
				Arg.Is<RedisKey>($"{countKeyPrefix}:{events[0].Date:O}"))
			.Returns(3);

		_ = fakeRedisDatabase.StringGetAsync(
				Arg.Is<RedisKey>($"{countKeyPrefix}:{events[1].Date:O}"))
			.Returns(4);

		_ = fakeRedisDatabase.StringDecrementAsync(
				Arg.Is<RedisKey>($"{countKeyPrefix}:{events[0].Date:O}"))
			.Returns(2);

		var sut = new RouletteCountService(fakeRedisDatabase, fakeEventQueryService, fakeUsedCountQueryService, fakeSystemClock);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);
	}

	//[Test]
	//public async Task Get_RouletteCount_When_Miss_Redis_Cache_And_Two_Events_Returns_And_First_Events_Has_Run_Out_Of_Roulette_Count()
	//{
	//	var fakeRedisDatabase = Substitute.For<IDatabase>();
	//	var fakeEventQueryService =
	//		Substitute.For<IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo>>();
	//	var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();

	//	var promotionId = Guid.NewGuid();
	//	var playerCode = "player1";
	//	var brandId = Guid.NewGuid();

	//	var expected = new RouletteCountInfo(Guid.NewGuid(), 1);

	//	var events = new DepositAccumulateRouletteEventInfo[]
	//	{
	//		new DepositAccumulateRouletteEventInfo(
	//			Guid.NewGuid(),
	//			playerCode,
	//			brandId,
	//			promotionId,
	//			DateOnly.Parse("2022-08-09"),
	//			2,
	//			1500),
	//		new DepositAccumulateRouletteEventInfo(
	//			expected.DepositAccumulateRouletteEventId!.Value,
	//			playerCode,
	//			brandId,
	//			promotionId,
	//			DateOnly.Parse("2022-08-10"),
	//			3,
	//			1000)
	//	};

	//	_ = fakeEventQueryService.QueryAsync(
	//			Arg.Is<GetDepositAccumulateEventsQuery>(x =>
	//				x.PromotionId == promotionId && x.PlayerCode == playerCode))
	//		.Returns(events.ToAsyncEnumerable());

	//	_ = fakeUsedCountQueryService.GetAsync(
	//			Arg.Is<RouletteUsedCountQuery>(x =>
	//				x.PromotionId == promotionId && x.PlayerCode == playerCode
	//											 && x.DepositAccumulateRouletteEventId ==
	//											 events[0].EventId))
	//		.Returns(2);

	//	_ = fakeUsedCountQueryService.GetAsync(
	//			Arg.Is<RouletteUsedCountQuery>(x =>
	//				x.PromotionId == promotionId && x.PlayerCode == playerCode
	//											 && x.DepositAccumulateRouletteEventId ==
	//											 expected.DepositAccumulateRouletteEventId))
	//		.Returns(2);

	//	var sut = new RouletteCountService(fakeRedisDatabase, fakeEventQueryService, fakeUsedCountQueryService);

	//	var actual = await sut.GetRemainingRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

	//	Assert.AreEqual(expected, actual);

	//	_ = fakeEventQueryService
	//		.Received(1)
	//		.QueryAsync(Arg.Is<GetDepositAccumulateEventsQuery>(x =>
	//			x.PromotionId == promotionId && x.PlayerCode == playerCode));

	//	_ = fakeRedisDatabase
	//		.Received(1)
	//		.StringSetAsync(
	//			Arg.Is<RedisKey>(x => x.ToString().StartsWith("Promotion.DepositAccumulateRoulette:EventId")),
	//			Arg.Is((RedisValue)expected.DepositAccumulateRouletteEventId!.Value.ToString("D")),
	//			Arg.Any<TimeSpan>(),
	//			Arg.Any<When>());

	//	var countKey =
	//		$"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}:{expected.DepositAccumulateRouletteEventId}";

	//	_ = fakeRedisDatabase
	//		.Received(1)
	//		.StringSetAsync(
	//			Arg.Is((RedisKey)countKey),
	//			Arg.Is((RedisValue)expected.RouletteCount),
	//			Arg.Any<TimeSpan>(),
	//			Arg.Any<When>());
	//}

	//[Test]
	//public async Task Get_RouletteCount_When_Miss_Redis_Cache_And_No_Events_Returns()
	//{
	//	var fakeRedisDatabase = Substitute.For<IDatabase>();
	//	var fakeEventQueryService =
	//		Substitute.For<IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo>>();
	//	var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();

	//	var promotionId = Guid.NewGuid();
	//	var playerCode = "player1";
	//	var brandId = Guid.NewGuid();

	//	var expected = new RouletteCountInfo(null, 0);

	//	_ = fakeEventQueryService.QueryAsync(
	//			Arg.Is<GetDepositAccumulateEventsQuery>(x =>
	//				x.PromotionId == promotionId && x.PlayerCode == playerCode))
	//		.Returns(Array.Empty<DepositAccumulateRouletteEventInfo>().ToAsyncEnumerable());

	//	var sut = new RouletteCountService(fakeRedisDatabase, fakeEventQueryService, fakeUsedCountQueryService);

	//	var actual = await sut.GetRemainingRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

	//	Assert.AreEqual(expected, actual);

	//	_ = fakeEventQueryService
	//		.Received(1)
	//		.QueryAsync(Arg.Is<GetDepositAccumulateEventsQuery>(x =>
	//			x.PromotionId == promotionId && x.PlayerCode == playerCode));

	//	_ = fakeRedisDatabase
	//		.Received(1)
	//		.StringSetAsync(
	//			Arg.Is<RedisKey>(x => x.ToString().StartsWith("Promotion.DepositAccumulateRoulette:EventId")),
	//			Arg.Is((RedisValue)Guid.Empty.ToString("D")),
	//			Arg.Any<TimeSpan>(),
	//			Arg.Any<When>());

	//	var countKey =
	//		$"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}:null";

	//	_ = fakeRedisDatabase
	//		.Received(1)
	//		.StringSetAsync(
	//			Arg.Is((RedisKey)countKey),
	//			Arg.Is((RedisValue)0),
	//			Arg.Any<TimeSpan>(),
	//			Arg.Any<When>());
	//}
}
