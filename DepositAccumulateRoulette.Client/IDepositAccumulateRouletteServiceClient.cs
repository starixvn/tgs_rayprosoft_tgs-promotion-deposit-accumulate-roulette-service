namespace Tgs.DepositAccumulateRoulette.Clients;

public interface IDepositAccumulateRouletteServiceClient
{
	IAsyncEnumerable<RouletteRecord> GetRouletteRecordsAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<RouletteRecord> GetRouletteRecordsAsync(
		Guid brandId,
		RouletteRecordQuery query,
		CancellationToken cancellationToken = default);

	ValueTask<RouletteRecord?> GetRouletteRecordAsync(
		Guid brandId,
		Guid recordId,
		CancellationToken cancellationToken = default);
}
