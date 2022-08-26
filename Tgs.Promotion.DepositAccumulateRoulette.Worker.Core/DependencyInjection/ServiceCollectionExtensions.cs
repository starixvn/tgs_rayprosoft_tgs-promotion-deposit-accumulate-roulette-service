using System.Reflection;
using Tgs.Promotion.DepositAccumulateRoulette.Worker;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDepositAccumulateRouletteWorkerCore(
		this IServiceCollection services)
	{
		_ = services
			.RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient);

		_ = services
			.AddSingleton<IDistributePlanHasher, DistributePlanHasher>()
			.AddSingleton<IStatisticsService, StatisticsService>()
			.AddSingleton<IShuffleService, ShuffleService>();

		_ = services
			.AddTransient<IRouletteCountService, RouletteCountService>();

		_ = services.AddSingleton<RouletteTaskQueueManager>()
			.AddHostedService<RouletteTaskBackgroundService>();

		return services;
	}
}
