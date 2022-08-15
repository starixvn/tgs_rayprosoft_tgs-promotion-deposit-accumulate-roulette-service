using System.Reflection;
using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Core;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDepositAccumulateRouletteCore(
		this IServiceCollection services,
		Func<IServiceProvider, IDatabase> resolveRedisDatabase)
	{
		_ = services
			.RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient);

		_ = services
			.AddSingleton<IDistributePlanHasher, DistributePlanHasher>()
			.AddSingleton<IStatisticsService, StatisticsService>()
			.AddSingleton<IShuffleService, ShuffleService>();

		_ = services
			.AddTransient<IRouletteCountService>(sp =>
			{
				var database = resolveRedisDatabase(sp);

				return ActivatorUtilities.CreateInstance<RouletteCountService>(sp, database);
			});

		return services;
	}
}
