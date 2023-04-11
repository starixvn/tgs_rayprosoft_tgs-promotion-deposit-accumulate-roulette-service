using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Cache;

internal class MemoryCachedGetPromotionDistributePlanQueryService : IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>
{
	private readonly IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo> _sourceService;
	private readonly IMemoryCache _memoryCache;
	private readonly ILogger<MemoryCachedGetPromotionDistributePlanQueryService> _logger;

	public MemoryCachedGetPromotionDistributePlanQueryService(
		IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo> sourceService,
		IMemoryCache memoryCache,
		ILogger<MemoryCachedGetPromotionDistributePlanQueryService> logger)
	{
		_sourceService = sourceService ?? throw new ArgumentNullException(nameof(sourceService));
		_memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask<PromotionDistributePlanInfo> GetAsync(
		GetPromotionDistributePlanQuery query,
		CancellationToken cancellationToken = default)
	{
		var cacheKey = $"DistributePlan:{query.PromotionId}:{query.ConditionHash}";

		var plan = (await _memoryCache.GetOrCreateAsync(cacheKey, async entry =>
		{
			_ = entry.SetAbsoluteExpiration(TimeSpan.FromHours(8D));

			return await _sourceService.GetAsync(query, cancellationToken).ConfigureAwait(false);
		}).ConfigureAwait(false))!;

		_logger.LogInformation("Query Promotion Distribution Plan by Key: {key}, planId: {planId}", cacheKey, plan.Id);

		return plan;
	}
}
