using System.Reflection;
using Tgs.Promotion.DepositAccumulateRouletteServices;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDepositAccumulateRouletteCore(
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

		_ = services
			.AddTransient<IRouletteHandleService, RouletteHandleService>();

		return services;
	}
}
