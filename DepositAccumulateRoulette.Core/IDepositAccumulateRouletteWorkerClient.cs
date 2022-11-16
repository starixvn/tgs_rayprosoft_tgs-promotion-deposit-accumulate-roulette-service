namespace Tgs.Promotion.DepositAccumulateRouletteService;

public interface IDepositAccumulateRouletteWorkerClient
{
	IAsyncEnumerable<RouletteRecordInfo> QueryRouletteRecordsAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<RouletteRecordInfo> QueryRouletteRecordsAsync(
		Guid brandId,
		RouletteRecordQueryConditions conditions,
		Paging paging,
		CancellationToken cancellationToken = default);
}
