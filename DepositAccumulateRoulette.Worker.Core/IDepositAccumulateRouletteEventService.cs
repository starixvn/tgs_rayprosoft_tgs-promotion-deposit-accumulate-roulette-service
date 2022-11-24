namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal interface IDepositAccumulateRouletteEventService
{
	IAsyncEnumerable<DepositAccumulateRouletteEventInfo> QueryDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default);

	ValueTask<RouletteEventCountInfo?> GetAvailableRouletteCountEventAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default);
}
