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
	public async Task Decrease_RouletteCount()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRedisRepository = Substitute.For<IRedisRepository>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var date = DateOnly.Parse("2022-08-11");

		var sut = new StatisticsService(fakeSystemClock, fakeTgsRepository, fakeRedisRepository);

		await sut.DecreaseRouletteCountAsync(promotionId, playerCode, date).ConfigureAwait(false);

		_ = fakeRedisRepository
			.Received(1)
			.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(date));
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
}
