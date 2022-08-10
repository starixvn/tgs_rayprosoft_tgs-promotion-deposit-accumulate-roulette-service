namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

internal interface IRouletteCountService
{
	public ValueTask<RouletteCountInfo> GetRemainingRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default);

	ValueTask<int> DecreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		Guid depositAccumulateRouletteEventId,
		CancellationToken cancellationToken = default);

	ValueTask<int> IncreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		Guid depositAccumulateRouletteEventId,
		CancellationToken cancellationToken = default);
}
