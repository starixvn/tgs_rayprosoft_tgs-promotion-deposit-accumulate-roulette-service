namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public interface ITgsRepository
{
	IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		DateOnly eventDate);

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

	IAsyncEnumerable<Guid> GetAvailablePromotionIdsAsync(
		Guid brandId,
		string playerCode,
		CancellationToken cancellationToken);
}
