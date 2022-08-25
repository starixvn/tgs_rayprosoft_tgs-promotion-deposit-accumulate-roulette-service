using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Worker;

namespace Tgs.Promotion.DepositAccumulateRoulette.Repositories;

internal class RouletteCountRepository : IRouletteCountRepository
{
	private readonly IDatabase _redisDatabase;

	public RouletteCountRepository(
		IDatabase redisDatabase)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
	}

	public async ValueTask<int?> GetRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date)
	{
		var redisKey = GetRedisKey(promotionId, playerCode, date);

		var value = await _redisDatabase.StringGetAsync(redisKey).ConfigureAwait(false);

		return value.HasValue ? (int)value! : null;
	}

	public async ValueTask<int> DecreaseRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date)
	{
		var redisKey = GetRedisKey(promotionId, playerCode, date);

		return (int)await _redisDatabase.StringDecrementAsync(redisKey).ConfigureAwait(false);
	}

	public async ValueTask<int> IncreaseRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date)
	{
		var redisKey = GetRedisKey(promotionId, playerCode, date);

		return (int)await _redisDatabase.StringIncrementAsync(redisKey).ConfigureAwait(false);
	}

	public async ValueTask<bool> SetRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date, int rouletteCount)
	{
		var redisKey = GetRedisKey(promotionId, playerCode, date);

		return await _redisDatabase
			.StringSetAsync(redisKey, rouletteCount, TimeSpan.FromDays(2))
			.ConfigureAwait(false);
	}

	private static string GetRedisKey(Guid promotionId, string playerCode, DateOnly date)
		=> $"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}:{date:O}";
}
