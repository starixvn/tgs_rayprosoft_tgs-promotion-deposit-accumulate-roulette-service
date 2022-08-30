using Microsoft.Extensions.Caching.Memory;
using Tgs.Promotion.DepositAccumulateRoulette.Worker;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Cache;

internal class MemoryCachedGetDepositAccumulateRoulettePromotionQueryService
	: IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>
{
	private readonly IMemoryCache _memoryCache;
	private readonly IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?> _sourceService;

	public MemoryCachedGetDepositAccumulateRoulettePromotionQueryService(
		IMemoryCache memoryCache,
		IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?> sourceService)
	{
		_memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
		_sourceService = sourceService ?? throw new ArgumentNullException(nameof(sourceService));
	}

	public async ValueTask<DepositAccumulateRoulettePromotion?> GetAsync(
		GetDepositAccumulateRoulettePromotionQuery query,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"Promotion:{query.PromotionId}";

		var promotion = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
		{
			_ = entry.SetAbsoluteExpiration(TimeSpan.FromDays(1D));

			return await _sourceService.GetAsync(query, cancellationToken).ConfigureAwait(false);
		}).ConfigureAwait(false);

		return promotion;
	}
}
