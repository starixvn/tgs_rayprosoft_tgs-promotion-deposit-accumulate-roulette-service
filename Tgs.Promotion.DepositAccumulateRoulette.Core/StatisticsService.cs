namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

internal class StatisticsService : IStatisticsService
{
	private readonly IStatisticsRepository _statisticsRepository;

	public StatisticsService(
		IStatisticsRepository statisticsRepository)
	{
		_statisticsRepository = statisticsRepository ?? throw new ArgumentNullException(nameof(statisticsRepository));
	}

	public ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default)
		=> _statisticsRepository
			.IncrementTotalDistributeAmountAsync(promotionId, amount, cancellationToken);
}
