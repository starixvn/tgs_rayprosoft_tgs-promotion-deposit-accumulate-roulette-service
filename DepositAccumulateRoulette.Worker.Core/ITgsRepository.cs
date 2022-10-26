namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public interface ITgsRepository
{
	IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		DateOnly eventDate);

	IAsyncEnumerable<Guid> GetPlayerAvailablePromotionIdsAsync(
		string playerCode,
		DateOnly queryDate);

	ValueTask<int> GetRouletteUsedCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly eventDate,
		CancellationToken cancellationToken = default);

	ValueTask<Guid> AddRouletteRecordAsync(
		Guid promotionId,
		string playerCode,
		Guid brandId,
		DateOnly eventDate,
		decimal bonus,
		int itemIndex,
		CancellationToken cancellationToken = default);

	ValueTask<RouletteRecordInfo?> GetRouletteRecordInfoAsync(
		Guid rouletteRecordId,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<RouletteRecordInfo> GetRouletteRecordInfosAsync(
		Guid promotionId,
		string playerCode,
		DateOnly eventDate);
}
