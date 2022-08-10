namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

internal interface IStatisticsService
{
	ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default);
}
