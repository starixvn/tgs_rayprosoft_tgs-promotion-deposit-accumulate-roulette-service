namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal interface IStatisticsService
{
	ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default);

	ValueTask<(bool Success, int AvailableCount)> DecreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly countDistributeDate,
		CancellationToken cancellationToken = default);

	ValueTask<int> IncreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly countDistributeDate,
		CancellationToken cancellationToken = default);
}
