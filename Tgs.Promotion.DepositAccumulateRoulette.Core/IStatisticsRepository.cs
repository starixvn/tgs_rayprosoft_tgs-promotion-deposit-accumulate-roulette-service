namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

public interface IStatisticsRepository
{
	ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default);
}
