namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal interface IRouletteCountService
{
	ValueTask<int> GetAvailableRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default);

	ValueTask<RouletteCountInfo> DecreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default);

	ValueTask IncreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly countDistributeDate,
		CancellationToken cancellationToken = default);
}
