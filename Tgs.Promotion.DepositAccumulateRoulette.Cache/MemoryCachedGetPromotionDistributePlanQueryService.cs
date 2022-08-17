using Microsoft.Extensions.Caching.Memory;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRouletteServices.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Cache;

internal class MemoryCachedGetPromotionDistributePlanQueryService : IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>
{
	private readonly IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo> _sourceService;
	private readonly IMemoryCache _memoryCache;

	public MemoryCachedGetPromotionDistributePlanQueryService(
		IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo> sourceService,
		IMemoryCache memoryCache)
	{
		_sourceService = sourceService ?? throw new ArgumentNullException(nameof(sourceService));
		_memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
	}

	public async ValueTask<PromotionDistributePlanInfo> GetAsync(
		GetPromotionDistributePlanQuery query,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"DistributePlan:{query.PromotionId}:{query.ConditionHash}";

		var plan = await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
		{
			_ = entry.SetAbsoluteExpiration(TimeSpan.FromHours(8D));

			return await _sourceService.GetAsync(query, cancellationToken).ConfigureAwait(false);
		}).ConfigureAwait(false);

		return plan;
	}
}
