namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal interface IStatisticsService
{
	ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default);

	ValueTask<(bool Success, int UsedCount)> IncreaseRouletteUsedCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly countDistributeDate,
		CancellationToken cancellationToken = default);
}
