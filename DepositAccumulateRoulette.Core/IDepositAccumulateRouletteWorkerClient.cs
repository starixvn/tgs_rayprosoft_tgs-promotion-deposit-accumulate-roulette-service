namespace Tgs.Promotion.DepositAccumulateRouletteService;

public interface IDepositAccumulateRouletteWorkerClient
{
	IAsyncEnumerable<RouletteRecordInfo> GetRouletteRecordsAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default);
}
