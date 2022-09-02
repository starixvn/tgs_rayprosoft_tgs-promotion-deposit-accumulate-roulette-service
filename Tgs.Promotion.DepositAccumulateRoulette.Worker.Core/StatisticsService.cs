using System.Runtime.CompilerServices;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class StatisticsService : IStatisticsService
{
	private readonly IRedisRepository _redisRepository;

	public StatisticsService(
		IRedisRepository redisRepository)
	{
		_redisRepository = redisRepository ?? throw new ArgumentNullException(nameof(redisRepository));
	}

	public ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default)
		=> _redisRepository
			.IncrementTotalDistributeAmountAsync(promotionId, amount, cancellationToken);

	public async ValueTask<(bool Success, int AvailableCount)> DecreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly countDistributeDate,
		CancellationToken cancellationToken = default)
	{
		var decreasedCount = await _redisRepository
			.DecreaseRouletteCountAsync(promotionId, playerCode, countDistributeDate)
			.ConfigureAwait(false);

		if (decreasedCount >= 0)
			return (true, decreasedCount);

		var increasedCount = await _redisRepository
			.IncreaseRouletteCountAsync(promotionId, playerCode, countDistributeDate)
			.ConfigureAwait(false);

		return (false, increasedCount);
	}

	public async ValueTask<int> IncreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly countDistributeDate,
		CancellationToken cancellationToken = default)
		=> await _redisRepository
			.IncreaseRouletteCountAsync(promotionId, playerCode, countDistributeDate)
			.ConfigureAwait(false);
}
