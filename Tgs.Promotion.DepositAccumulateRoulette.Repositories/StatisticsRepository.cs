using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRouletteServices;

namespace Tgs.Promotion.DepositAccumulateRoulette.Repositories;

internal class StatisticsRepository : IStatisticsRepository
{
	private static readonly decimal _ExchangeRate = 1_000_000M;

	private readonly IDatabase _redisDatabase;

	public StatisticsRepository(
		IDatabase redisDatabase)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
	}

	public async ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default)
	{
		var redisKey = $"Promotion:DepositAccumulateRoulette:TotalUsed:{promotionId}";

		var returnAmount = await _redisDatabase
			.StringIncrementAsync(redisKey, (long)Math.Floor(amount * _ExchangeRate))
			.ConfigureAwait(false);

		return returnAmount / _ExchangeRate;
	}
}
