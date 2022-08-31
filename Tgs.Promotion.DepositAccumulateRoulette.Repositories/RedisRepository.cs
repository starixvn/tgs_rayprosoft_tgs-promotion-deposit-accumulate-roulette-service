using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Worker;

namespace Tgs.Promotion.DepositAccumulateRoulette.Repositories;

internal class RedisRepository : IRedisRepository
{
	private static readonly string _KeyPrefix = "Promotion:DepositAccumulateRoulette";
	private static readonly decimal _ExchangeRate = 1_000_000M;

	private readonly IDatabase _redisDatabase;

	public RedisRepository(
		IDatabase redisDatabase)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
	}

	public async ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default)
	{
		var redisKey = $"{_KeyPrefix}:TotalUsed:{promotionId}";

		var returnAmount = await _redisDatabase
			.StringIncrementAsync(redisKey, (long)Math.Floor(amount * _ExchangeRate))
			.ConfigureAwait(false);

		return returnAmount / _ExchangeRate;
	}

	public async ValueTask<int?> GetRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date)
	{
		var redisKey = GetRouletteCountRedisKey(promotionId, playerCode, date);

		var value = await _redisDatabase.StringGetAsync(redisKey).ConfigureAwait(false);

		return value.HasValue ? (int)value! : null;
	}

	public async ValueTask<int> DecreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date)
	{
		var redisKey = GetRouletteCountRedisKey(promotionId, playerCode, date);

		return (int)await _redisDatabase.StringDecrementAsync(redisKey).ConfigureAwait(false);
	}

	public async ValueTask<int> IncreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date)
	{
		var redisKey = GetRouletteCountRedisKey(promotionId, playerCode, date);

		return (int)await _redisDatabase.StringIncrementAsync(redisKey).ConfigureAwait(false);
	}

	public async ValueTask<bool> SetRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date,
		int rouletteCount)
	{
		var redisKey = GetRouletteCountRedisKey(promotionId, playerCode, date);

		return await _redisDatabase
			.StringSetAsync(redisKey, rouletteCount, TimeSpan.FromDays(2))
			.ConfigureAwait(false);
	}

	public IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(Guid promotionId, string playerCode,
		CancellationToken cancellationToken = default) =>
		throw new NotImplementedException();

	public async ValueTask ClearExpiredDepositAccumulateRouletteEventsAsync(Guid promotionId, string playerCode) => throw new NotImplementedException();

	private static string GetRouletteCountRedisKey(Guid promotionId, string playerCode, DateOnly date)
		=> $"{_KeyPrefix}:RouletteCount:{promotionId}:{playerCode}:{date:O}";
}
