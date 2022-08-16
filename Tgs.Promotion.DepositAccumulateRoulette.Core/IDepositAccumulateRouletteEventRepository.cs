namespace Tgs.Promotion.DepositAccumulateRouletteServices;

public interface IDepositAccumulateRouletteEventRepository
{
	IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default);
}
