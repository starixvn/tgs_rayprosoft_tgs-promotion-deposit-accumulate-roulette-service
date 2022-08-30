using System.Text.Json;
using DateOnlyTimeOnly.AspNet.Converters;
using NSubstitute;
using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Worker;

namespace Tgs.Promotion.DepositAccumulateRoulette.Repositories.UnitTests;

public class DepositAccumulateRouletteEventRepositoryTests
{
	[Test]
	public async Task Get_Data_When_Hit_Cache()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var fakeSystemClock = Substitute.For<ISystemClock>();

		var jsonSerializerOptions = new JsonSerializerOptions
		{
			Converters = { new DateOnlyJsonConverter() }
		};

		var brandId = Guid.NewGuid();
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

		var redisData = expected
			.Select(value => (RedisValue)JsonSerializer.Serialize(value, jsonSerializerOptions))
			.ToArray();

		var redisKey = $"Promotion:DepositAccumulateRoulette:DepositEvents:{promotionId}:{playerCode}";

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeRedisDatabase.SortedSetRangeByScoreAsync(
				key: Arg.Is<RedisKey>(redisKey),
				start: Arg.Is<double>(now.Ticks),
				exclude: Arg.Any<Exclude>(),
				order: Arg.Any<Order>())
			.Returns(redisData);

		var sut = new DepositAccumulateRouletteEventRepository(fakeRedisDatabase, fakeTgsRepository, fakeSystemClock);

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
		var fakeRedisDatabase = Substitute.For<IDatabase>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSystemClock = Substitute.For<ISystemClock>();

		var brandId = Guid.NewGuid();
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

		var redisKey = $"Promotion:DepositAccumulateRoulette:DepositEvents:{promotionId}:{playerCode}";

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeRedisDatabase.SortedSetRangeByScoreAsync(
				key: Arg.Is<RedisKey>(redisKey),
				start: Arg.Is<double>(now.Ticks),
				exclude: Arg.Any<Exclude>(),
				order: Arg.Any<Order>())
			.Returns(Array.Empty<RedisValue>());

		_ = fakeTgsRepository
			.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(queryDate))
			.Returns(expected.ToAsyncEnumerable());

		var sut = new DepositAccumulateRouletteEventRepository(fakeRedisDatabase, fakeTgsRepository, fakeSystemClock);

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

		_ = fakeRedisDatabase
			.Received(1)
			.SortedSetAddAsync(
				Arg.Is<RedisKey>(redisKey),
				Arg.Any<RedisValue>(),
				Arg.Is<double>(x => (long)x == new DateTimeOffset(2022, 8, 13, 8, 0, 0, TimeSpan.FromHours(8D)).Ticks));
	}

	[Test]
	public async Task Get_Data_When_Hit_Cache_And_CurrentDate_Is_Different_From_QueryDate()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSystemClock = Substitute.For<ISystemClock>();

		var jsonSerializerOptions = new JsonSerializerOptions
		{
			Converters = { new DateOnlyJsonConverter() }
		};

		var brandId = Guid.NewGuid();
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

		var redisData = expected
			.Select(value => (RedisValue)JsonSerializer.Serialize(value, jsonSerializerOptions))
			.ToArray();

		var redisKey = $"Promotion:DepositAccumulateRoulette:DepositEvents:{promotionId}:{playerCode}";

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeRedisDatabase.SortedSetRangeByScoreAsync(
				key: Arg.Is<RedisKey>(redisKey),
				start: Arg.Is<double>(now.Ticks),
				exclude: Arg.Any<Exclude>(),
				order: Arg.Any<Order>())
			.Returns(redisData);

		var sut = new DepositAccumulateRouletteEventRepository(fakeRedisDatabase, fakeTgsRepository, fakeSystemClock);

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
		var fakeRedisDatabase = Substitute.For<IDatabase>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeSystemClock = Substitute.For<ISystemClock>();

		var brandId = Guid.NewGuid();
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

		var redisKey = $"Promotion:DepositAccumulateRoulette:DepositEvents:{promotionId}:{playerCode}";

		_ = fakeSystemClock.UtcNow
			.Returns(now);

		_ = fakeRedisDatabase.SortedSetRangeByScoreAsync(
				key: Arg.Is<RedisKey>(redisKey),
				start: Arg.Is<double>(now.Ticks),
				exclude: Arg.Any<Exclude>(),
				order: Arg.Any<Order>())
			.Returns(Array.Empty<RedisValue>());

		_ = fakeTgsRepository
			.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(queryDate))
			.Returns(expected.ToAsyncEnumerable());

		var sut = new DepositAccumulateRouletteEventRepository(fakeRedisDatabase, fakeTgsRepository, fakeSystemClock);

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

		_ = fakeRedisDatabase
			.Received(2)
			.SortedSetAddAsync(
				Arg.Is<RedisKey>(redisKey),
				Arg.Any<RedisValue>(),
				Arg.Is<double>(x => (long)x == now.AddMinutes(5D).Ticks));
	}
}
