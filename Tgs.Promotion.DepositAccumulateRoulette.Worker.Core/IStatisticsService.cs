namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal interface IStatisticsService
{
	ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default);
}
