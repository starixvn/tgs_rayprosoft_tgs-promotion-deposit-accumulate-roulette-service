using NSubstitute;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class StatisticsServiceTests
{
	[Test]
	public async Task Increase_Total_Used_Amount()
	{
		var fakeStoreRepository = Substitute.For<IStoreRepository>();

		var promotionId = Guid.NewGuid();
		var distributeAmount = 20m;

		var expected = 100m;

		_ = fakeStoreRepository.IncrementTotalDistributeAmountAsync(
				Arg.Is(promotionId),
				Arg.Is(distributeAmount))
			.Returns(expected);

		var sut = new StatisticsService(fakeStoreRepository);

		var actual = await sut.IncrementTotalDistributeAmountAsync(promotionId, distributeAmount);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Increase_RouletteUsedCount_If_Count_Is_Stored()
	{
		var fakeStoreRepository = Substitute.For<IStoreRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var date = DateOnly.Parse("2022-08-11");

		_ = fakeStoreRepository
			.GetRouletteUsedCountAsync(promotionId, playerCode, date)
			.Returns(1);

		var sut = new StatisticsService(fakeStoreRepository);

		var (actual, _) = await sut.IncreaseRouletteUsedCountAsync(promotionId, playerCode, date).ConfigureAwait(false);

		Assert.IsTrue(actual);

		_ = fakeStoreRepository
			.Received(1)
			.IncreaseRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(date));
	}

	[Test]
	public async Task Increase_RouletteUsedCount_If_Count_Is_Not_Stored()
	{
		var fakeStoreRepository = Substitute.For<IStoreRepository>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var date = DateOnly.Parse("2022-08-11");

		_ = fakeStoreRepository
			.GetRouletteUsedCountAsync(promotionId, playerCode, date)
			.Returns((int?)null);

		var sut = new StatisticsService(fakeStoreRepository);

		var (actual, _) = await sut.IncreaseRouletteUsedCountAsync(promotionId, playerCode, date).ConfigureAwait(false);

		Assert.IsFalse(actual);

		_ = fakeStoreRepository
			.DidNotReceive()
			.IncreaseRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(date));
	}
}
