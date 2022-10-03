namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal interface IDepositAccumulateRouletteEventService
{
	IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default);
}
