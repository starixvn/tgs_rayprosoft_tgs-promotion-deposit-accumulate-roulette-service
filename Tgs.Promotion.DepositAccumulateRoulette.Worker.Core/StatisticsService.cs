namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class StatisticsService : IStatisticsService
{
	private readonly IStoreRepository _storeRepository;

	public StatisticsService(
		IStoreRepository storeRepository)
	{
		_storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
	}

	public ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default)
		=> _storeRepository
			.IncrementTotalDistributeAmountAsync(promotionId, amount, cancellationToken);

	public async ValueTask<(bool Success, int AvailableCount)> DecreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly countDistributeDate,
		CancellationToken cancellationToken = default)
	{
		var decreasedCount = await _storeRepository
			.DecreaseRouletteCountAsync(promotionId, playerCode, countDistributeDate)
			.ConfigureAwait(false);

		if (decreasedCount >= 0)
			return (true, decreasedCount);

		var increasedCount = await _storeRepository
			.IncreaseRouletteCountAsync(promotionId, playerCode, countDistributeDate)
			.ConfigureAwait(false);

		return (false, increasedCount);
	}

	public async ValueTask<int> IncreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly countDistributeDate,
		CancellationToken cancellationToken = default)
		=> await _storeRepository
			.IncreaseRouletteCountAsync(promotionId, playerCode, countDistributeDate)
			.ConfigureAwait(false);
}
