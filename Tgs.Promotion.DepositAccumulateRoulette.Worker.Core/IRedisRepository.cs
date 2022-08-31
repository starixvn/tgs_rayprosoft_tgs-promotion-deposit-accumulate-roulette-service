namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public interface IRedisRepository
{
	ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default);

	ValueTask<int?> GetRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date);

	ValueTask<int> DecreaseRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date);

	ValueTask<int> IncreaseRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date);

	ValueTask<bool> SetRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date, int rouletteCount);

	IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		DateTimeOffset queryDate);

	ValueTask<bool> SetDepositAccumulateRouletteEventInfoAsync(Guid promotionId, string playerCode, DateTimeOffset expireDate, DepositAccumulateRouletteEventInfo eventInfo);

	ValueTask ClearExpiredDepositAccumulateRouletteEventsAsync(Guid promotionId, string playerCode);
}
