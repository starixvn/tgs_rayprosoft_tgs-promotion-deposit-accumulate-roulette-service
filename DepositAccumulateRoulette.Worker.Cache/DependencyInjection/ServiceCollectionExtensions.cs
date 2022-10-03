using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Worker;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.Cache;
using Tgs.ServiceUtility;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDepositAccumulateRouletteWorkerCache(
		this IServiceCollection services)
	{
		_ = services
			.Decorate<IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>,
				MemoryCachedGetPromotionDistributePlanQueryService>()
			.Decorate<IShuffleService, MemoryCachedShuffleService>();

		return services;
	}
}
