using Microsoft.Extensions.Caching.Memory;
using Tgs.Promotion.DepositAccumulateRouletteService.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRouletteService;

internal class MemoryCachedGetDepositAccumulateRoulettePromotionQueryService : IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotionInfo?>
{
	private readonly IMemoryCache _memoryCache;
	private readonly IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotionInfo?> _sourceService;

	public MemoryCachedGetDepositAccumulateRoulettePromotionQueryService(
		IMemoryCache memoryCache,
		IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotionInfo?> sourceService)
	{
		_memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
		_sourceService = sourceService ?? throw new ArgumentNullException(nameof(sourceService));
	}

	public async ValueTask<DepositAccumulateRoulettePromotionInfo?> GetAsync(
		GetDepositAccumulateRoulettePromotionQuery query,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"PromotionInfo:{query.PromotionId}";

		return await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
		{
			entry.AbsoluteExpiration = DateTimeOffset.Now.AddDays(1);

			return await _sourceService.GetAsync(query, cancellationToken).ConfigureAwait(false);
		}).ConfigureAwait(false);
	}
}
