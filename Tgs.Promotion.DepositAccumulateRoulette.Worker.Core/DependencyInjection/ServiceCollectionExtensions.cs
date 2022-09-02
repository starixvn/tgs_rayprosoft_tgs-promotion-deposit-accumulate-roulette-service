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
			.AddTransient<IStatisticsService, StatisticsService>()
			.AddTransient<IDepositAccumulateRouletteEventService, DepositAccumulateRouletteEventService>();

		_ = services
			.AddSingleton<ISystemClock, SystemClock>()
			.AddSingleton<IDistributePlanHasher, DistributePlanHasher>()

			.AddSingleton<IShuffleService, ShuffleService>();

		_ = services.AddSingleton<RouletteTaskQueueManager>()
			.AddHostedService<RouletteTaskBackgroundService>();

		return services;
	}
}
