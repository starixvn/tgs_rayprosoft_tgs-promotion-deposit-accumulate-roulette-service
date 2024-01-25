namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class StatisticsService : IStatisticsService
{
	private readonly IStoreRepository _storeRepository;

	public StatisticsService(
		IStoreRepository storeRepository) => _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));

	public ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default)
		=> _storeRepository
			.IncrementTotalDistributeAmountAsync(promotionId, amount, cancellationToken);

	public async ValueTask<(bool Success, int UsedCount)> IncreaseRouletteUsedCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly countDistributeDate,
		CancellationToken cancellationToken = default)
	{
		var beforeCount = await _storeRepository
			.GetRouletteUsedCountAsync(promotionId, playerCode, countDistributeDate)
			.ConfigureAwait(false);

		if (!beforeCount.HasValue)
			return (false, 0);

		var usedCount = await _storeRepository
			.IncreaseRouletteUsedCountAsync(promotionId, playerCode, countDistributeDate)
			.ConfigureAwait(false);

		return (true, usedCount);
	}
}
