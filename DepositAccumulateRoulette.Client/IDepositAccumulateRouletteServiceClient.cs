namespace Tgs.DepositAccumulateRoulette.Clients;

public interface IDepositAccumulateRouletteServiceClient
{
	IAsyncEnumerable<RouletteRecord> GetRouletteRecordsAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default);
}
