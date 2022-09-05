// ReSharper disable CheckNamespace

using System.Reflection;
using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRouletteService;
using Tgs.Promotion.DepositAccumulateRouletteService.ApplicationServices;
using Tgs.ServiceUtility;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDepositAccumulateRouletteService(
		this IServiceCollection services,
		Func<IServiceProvider, IDatabase> resolveRedisDatabase)
	{
		_ = services
			.RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient)
			.AddTransient<IRouletteHandleService>(sp =>
			{
				var database = resolveRedisDatabase(sp);

				return ActivatorUtilities.CreateInstance<RouletteHandleService>(sp, database);
			});

		_ = services.Decorate<
			IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotionInfo?>,
			MemoryCachedGetDepositAccumulateRoulettePromotionQueryService>();

		return services;
	}
}
