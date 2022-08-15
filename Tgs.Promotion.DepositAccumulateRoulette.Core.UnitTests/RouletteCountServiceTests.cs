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
		var fakeEventRepository = Substitute.For<IDepositAccumulateRouletteEventRepository>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();

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

		_ = fakeEventRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
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

		var sut = new RouletteCountService(fakeRedisDatabase, fakeEventRepository, fakeUsedCountQueryService);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Decrease_RouletteCount_When_Miss_Redis_Cache()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();
		var fakeEventRepository = Substitute.For<IDepositAccumulateRouletteEventRepository>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();

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

		_ = fakeEventRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
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

		var sut = new RouletteCountService(fakeRedisDatabase, fakeEventRepository, fakeUsedCountQueryService);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Decrease_RouletteCount_When_Event_Data_Is_Empty()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();
		var fakeEventRepository = Substitute.For<IDepositAccumulateRouletteEventRepository>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		var expected = new RouletteCountInfo(null, 0, 0, 0, 0);

		_ = fakeEventRepository.GetDepositAccumulateRouletteEventInfosAsync(
					Arg.Is(promotionId),
					Arg.Is(playerCode),
					Arg.Any<CancellationToken>())
			.Returns(Array.Empty<DepositAccumulateRouletteEventInfo>().ToAsyncEnumerable());

		var sut = new RouletteCountService(fakeRedisDatabase, fakeEventRepository, fakeUsedCountQueryService);

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
	public async Task Decrease_RouletteCount_With_Two_Events_Returns()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();
		var fakeEventRepository = Substitute.For<IDepositAccumulateRouletteEventRepository>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();

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

		_ = fakeEventRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
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

		var sut = new RouletteCountService(fakeRedisDatabase, fakeEventRepository, fakeUsedCountQueryService);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Decrease_RouletteCount_With_Two_Events_Returns_And_First_Event_Has_Run_Out_Of_Roulette_Count()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();
		var fakeEventRepository = Substitute.For<IDepositAccumulateRouletteEventRepository>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();

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

		var expected = new RouletteCountInfo(events[1], 4, 3, 4, 3);

		_ = fakeEventRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(events.ToAsyncEnumerable());

		var countKeyPrefix =
			$"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}";

		_ = fakeRedisDatabase.StringGetAsync(
				Arg.Is<RedisKey>($"{countKeyPrefix}:{events[0].Date:O}"))
			.Returns(0);

		_ = fakeRedisDatabase.StringGetAsync(
				Arg.Is<RedisKey>($"{countKeyPrefix}:{events[1].Date:O}"))
			.Returns(4);

		_ = fakeRedisDatabase.StringDecrementAsync(
				Arg.Is<RedisKey>($"{countKeyPrefix}:{events[1].Date:O}"))
			.Returns(3);

		var sut = new RouletteCountService(fakeRedisDatabase, fakeEventRepository, fakeUsedCountQueryService);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);

		_ = fakeRedisDatabase
			.DidNotReceive()
			.StringDecrementAsync(
				Arg.Is<RedisKey>($"{countKeyPrefix}:{events[0].Date:O}"));
	}

	[Test]
	public async Task Increase_RouletteCount()
	{
		var fakeRedisDatabase = Substitute.For<IDatabase>();
		var fakeEventRepository = Substitute.For<IDepositAccumulateRouletteEventRepository>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var date = DateOnly.Parse("2022-08-11");

		var sut = new RouletteCountService(fakeRedisDatabase, fakeEventRepository, fakeUsedCountQueryService);

		await sut.IncreaseRouletteCountAsync(promotionId, playerCode, date).ConfigureAwait(false);

		var countKey =
			$"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}:{date:O}";

		_ = fakeRedisDatabase
			.Received(1)
			.StringIncrementAsync(Arg.Is<RedisKey>(countKey));
	}
}
