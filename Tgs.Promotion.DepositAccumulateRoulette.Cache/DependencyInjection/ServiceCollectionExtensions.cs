using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Cache;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Core;
using Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDepositAccumulateRouletteCache(
		this IServiceCollection services)
	{
		_ = services
			.Decorate<IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>, MemoryCachedGetPromotionDistributePlanQueryService>();

		return services;
	}
}
