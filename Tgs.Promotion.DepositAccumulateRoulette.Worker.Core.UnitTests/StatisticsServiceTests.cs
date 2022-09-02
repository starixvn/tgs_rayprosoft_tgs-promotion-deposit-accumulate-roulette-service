using ExpectedObjects;
using NSubstitute;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class StatisticsServiceTests
{
	[Test]
	public async Task Increase_Total_Used_Amount()
	{
		var fakeRedisRepository = Substitute.For<IRedisRepository>();

		var promotionId = Guid.NewGuid();
		var distributeAmount = 20m;

		var expected = 100m;

		_ = fakeRedisRepository.IncrementTotalDistributeAmountAsync(
				Arg.Is(promotionId),
				Arg.Is(distributeAmount))
			.Returns(expected);

		var sut = new StatisticsService(fakeRedisRepository);

		var actual = await sut.IncrementTotalDistributeAmountAsync(promotionId, distributeAmount);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Decrease_RouletteCount_If_Returns_Is_Not_Negative()
	{
		var fakeRedisRepository = Substitute.For<IRedisRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var date = DateOnly.Parse("2022-08-11");

		_ = fakeRedisRepository.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(date))
			.Returns(2);

		var sut = new StatisticsService(fakeRedisRepository);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode, date).ConfigureAwait(false);

		Assert.AreEqual(true, actual.Success);
		Assert.AreEqual(2, actual.AvailableCount);

		_ = fakeRedisRepository
			.DidNotReceive()
			.IncreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(date));
	}

	[Test]
	public async Task Decrease_RouletteCount_If_Returns_Is_Negative()
	{
		var fakeRedisRepository = Substitute.For<IRedisRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var date = DateOnly.Parse("2022-08-11");

		_ = fakeRedisRepository.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(date))
			.Returns(-1);

		var sut = new StatisticsService(fakeRedisRepository);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode, date).ConfigureAwait(false);

		Assert.AreEqual(false, actual.Success);

		_ = fakeRedisRepository
			.Received(1)
			.IncreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(date));
	}

	[Test]
	public async Task Increase_RouletteCount()
	{
		var fakeRedisRepository = Substitute.For<IRedisRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var date = DateOnly.Parse("2022-08-11");

		var sut = new StatisticsService(fakeRedisRepository);

		await sut.IncreaseRouletteCountAsync(promotionId, playerCode, date).ConfigureAwait(false);

		_ = fakeRedisRepository
			.Received(1)
			.IncreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(date));
	}
}
