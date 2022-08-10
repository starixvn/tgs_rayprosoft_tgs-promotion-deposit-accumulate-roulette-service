using System.Text.Json;
using DateOnlyTimeOnly.AspNet.Converters;
using NSubstitute;
using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core.UnitTests;

internal class RedisCachedGetDepositAccumulateEventQueryServiceTests
{
	[Test]
	public async Task Get_Data_When_Hit_Cache()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();
		var fakeSourceService =
			Substitute.For<IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo>>();

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
				Guid.NewGuid(),
				playerCode,
				brandId,
				promotionId,
				DateOnly.Parse("2022-08-08"),
				4,
				1500),
			new DepositAccumulateRouletteEventInfo(
				Guid.NewGuid(),
				playerCode,
				brandId,
				promotionId,
				DateOnly.Parse("2022-08-07"),
				3,
				2200)
		};

		var redisData = expected
			.Select(value => (RedisValue)JsonSerializer.Serialize(value, jsonSerializerOptions))
			.ToArray();

		var query = new GetDepositAccumulateEventsQuery(promotionId, playerCode, DateOnly.Parse("2022-08-08"));

		var redisKey = $"Promotion:DepositAccumulateRoulette:DepositEventInfo:{query.PromotionId}:{query.PlayerCode}";

		_ = fakeRedisDatabase.SortedSetRangeByScoreAsync(
				key: Arg.Is<RedisKey>(redisKey),
				start: Arg.Any<double>(),
				exclude: Arg.Any<Exclude>(),
				order: Arg.Any<Order>())
			.Returns(redisData);

		//var sut = new RedisCachedGetDepositAccumulateEventQueryService(fakeRedisDatabase, fakeSourceService);

		//var actual = await sut.QueryAsync(query).ToArrayAsync().ConfigureAwait(false);

		//CollectionAssert.AreEqual(expected, actual);

		//_ = fakeSourceService
		//	.DidNotReceive()
		//	.QueryAsync(Arg.Is(query));
	}

	[Test]
	public async Task Get_Data_When_Miss_Cache()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();
		var fakeSourceService =
			Substitute.For<IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo>>();

		var brandId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var expected = new[]
		{
			new DepositAccumulateRouletteEventInfo(
				Guid.NewGuid(),
				playerCode,
				brandId,
				promotionId,
				DateOnly.Parse("2022-08-08"),
				4,
				1500)
		};

		var query = new GetDepositAccumulateEventsQuery(promotionId, playerCode, DateOnly.Parse("2022-08-08"));

		var redisKey = $"Promotion:DepositAccumulateRoulette:DepositEventInfo:{query.PromotionId}:{query.PlayerCode}";

		_ = fakeRedisDatabase.SortedSetRangeByScoreAsync(
				key: Arg.Is<RedisKey>(redisKey),
				start: Arg.Any<double>(),
				exclude: Arg.Any<Exclude>(),
				order: Arg.Any<Order>())
			.Returns(Array.Empty<RedisValue>());

		_ = fakeSourceService
			.QueryAsync(Arg.Is(query))
			.Returns(expected.ToAsyncEnumerable());

		//var sut = new RedisCachedGetDepositAccumulateEventQueryService(fakeRedisDatabase, fakeSourceService);

		//var actual = await sut.QueryAsync(query).ToArrayAsync().ConfigureAwait(false);

		//CollectionAssert.AreEqual(expected, actual);
	}
}
