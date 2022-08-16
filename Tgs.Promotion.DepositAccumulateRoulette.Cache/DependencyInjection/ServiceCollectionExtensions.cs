using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Cache;
using Tgs.Promotion.DepositAccumulateRouletteServices;
using Tgs.Promotion.DepositAccumulateRouletteServices.ApplicationServices;
using Tgs.ServiceUtility;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDepositAccumulateRouletteCache(
		this IServiceCollection services)
	{
		_ = services
			.Decorate<IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>,
				MemoryCachedGetPromotionDistributePlanQueryService>()
			.Decorate<IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>,
				MemoryCachedGetDepositAccumulateRoulettePromotionQueryService>();

		return services;
	}
}
