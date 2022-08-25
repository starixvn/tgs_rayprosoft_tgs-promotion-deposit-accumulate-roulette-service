namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public interface IDepositAccumulateRouletteEventRepository
{
	IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default);
}
