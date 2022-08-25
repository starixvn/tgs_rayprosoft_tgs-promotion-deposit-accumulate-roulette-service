namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public interface IStatisticsRepository
{
	ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default);
}
