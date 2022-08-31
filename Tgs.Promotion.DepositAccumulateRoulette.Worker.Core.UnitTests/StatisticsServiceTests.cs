using NSubstitute;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class StatisticsServiceTests
{
	[Test]
	public async Task Increase_Total_Used_Amount()
	{
		var redisRepository = Substitute.For<IRedisRepository>();

		var promotionId = Guid.NewGuid();
		var distributeAmount = 20m;

		var expected = 100m;

		_ = redisRepository.IncrementTotalDistributeAmountAsync(
				Arg.Is(promotionId),
				Arg.Is(distributeAmount))
			.Returns(expected);

		var sut = new StatisticsService(redisRepository);

		var actual = await sut.IncrementTotalDistributeAmountAsync(promotionId, distributeAmount);

		Assert.AreEqual(expected, actual);
	}
}
