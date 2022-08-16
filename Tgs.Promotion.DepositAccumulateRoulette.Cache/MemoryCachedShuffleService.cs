using Microsoft.Extensions.Caching.Memory;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRouletteServices;

namespace Tgs.Promotion.DepositAccumulateRoulette.Cache;

internal class MemoryCachedShuffleService : IShuffleService
{
	private readonly IMemoryCache _memoryCache;
	private readonly IShuffleService _shuffleService;

	public MemoryCachedShuffleService(
		IMemoryCache memoryCache,
		IShuffleService shuffleService)
	{
		_memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
		_shuffleService = shuffleService ?? throw new ArgumentNullException(nameof(shuffleService));
	}

	public async ValueTask<int[]> ShuffleAsync(Guid promotionId, IEnumerable<RouletteItem> items)
	{
		var key = $"Deck:{promotionId}";

		return await _memoryCache.GetOrCreateAsync(key, async entry =>
		{
			_ = entry.SetAbsoluteExpiration(TimeSpan.FromHours(6D));

			return await _shuffleService.ShuffleAsync(promotionId, items).ConfigureAwait(false);
		}).ConfigureAwait(false);
	}
}
