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
}
