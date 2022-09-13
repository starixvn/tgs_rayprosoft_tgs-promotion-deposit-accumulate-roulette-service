namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public interface IStoreRepository
{
	ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		DateTimeOffset queryDate);

	ValueTask<bool> SetDepositAccumulateRouletteEventInfoAsync(
		Guid promotionId,
		string playerCode,
		DateTimeOffset expireDate,
		DepositAccumulateRouletteEventInfo eventInfo);

	ValueTask ClearExpiredDepositAccumulateRouletteEventsAsync(Guid promotionId, string playerCode);

	ValueTask<int?> GetRouletteUsedCountAsync(Guid promotionId, string playerCode, DateOnly date);

	ValueTask<bool> SetRouletteUsedCountAsync(Guid promotionId, string playerCode, DateOnly date, int rouletteUsedCount);

	ValueTask<int> IncreaseRouletteUsedCountAsync(Guid promotionId, string playerCode, DateOnly date);
}
