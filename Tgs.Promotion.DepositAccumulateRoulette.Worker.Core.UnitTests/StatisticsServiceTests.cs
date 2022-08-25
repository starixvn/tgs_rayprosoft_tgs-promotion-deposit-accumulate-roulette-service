using NSubstitute;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class StatisticsServiceTests
{
	[Test]
	public async Task Increase_Total_Used_Amount()
	{
		var fakeStatisticsRepository = Substitute.For<IStatisticsRepository>();

		var promotionId = Guid.NewGuid();
		var distributeAmount = 20m;

		var expected = 100m;

		_ = fakeStatisticsRepository.IncrementTotalDistributeAmountAsync(
				Arg.Is(promotionId),
				Arg.Is(distributeAmount))
			.Returns(expected);

		var sut = new StatisticsService(fakeStatisticsRepository);

		var actual = await sut.IncrementTotalDistributeAmountAsync(promotionId, distributeAmount);

		Assert.AreEqual(expected, actual);
	}
}
