using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Core;

namespace Tgs.Promotion.DepositAccumulateRoulette.Cache;

internal class ClearDepositAccumulateRouletteEventCacheService : IClearDepositAccumulateRouletteEventCache
{
	private readonly IDatabase _redisDatabase;

	public ClearDepositAccumulateRouletteEventCacheService(
		IDatabase redisDatabase)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
	}

	public async ValueTask ClearCacheAsync(Guid promotionId, string playerCode)
	{
		var key = $"Promotion:DepositAccumulateRoulette:DepositEventInfo:{promotionId}:{playerCode}";

		var score = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8D)).Ticks;

		_ = await _redisDatabase.SortedSetRemoveRangeByScoreAsync(key, start: 0, stop: score, exclude: Exclude.Stop)
			.ConfigureAwait(false);
	}
}
