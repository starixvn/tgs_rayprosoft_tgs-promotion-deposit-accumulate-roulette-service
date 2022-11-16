using System.Threading;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public interface ITgsRepository
{
	IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		DateOnly eventDate,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<Guid> GetPlayerAvailablePromotionIdsAsync(
		string playerCode,
		DateOnly queryDate,
		CancellationToken cancellationToken = default);

	ValueTask<int> GetRouletteUsedCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly eventDate,
		CancellationToken cancellationToken = default);

	ValueTask<Guid> CreateRouletteRecordAsync(
		CreateRouletteRecordInfo createRecordInfo,
		CancellationToken cancellationToken = default);

	ValueTask<RouletteRecordInfo?> GetRouletteRecordInfoAsync(
		Guid rouletteRecordId,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<RouletteRecordInfo> GetRouletteRecordInfosAsync(
		Guid promotionId,
		string playerCode,
		DateOnly eventDate,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<RouletteRecordInfo> GetRouletteRecordInfosAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default);
}
