using System.Reflection;
using Microsoft.Extensions.Options;
using Tgs.Promotion.DepositAccumulateRoulette.Worker;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.Clients;

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
			.AddTransient<IPromotionService, PromotionService>()
			.AddTransient<IDepositAccumulateRouletteEventService, DepositAccumulateRouletteEventService>();

		_ = services
			.AddSingleton<ISystemClock, SystemClock>()
			.AddSingleton<IDistributePlanHasher, DistributePlanHasher>()

			.AddSingleton<IShuffleService, ShuffleService>();

		_ = services.AddSingleton<RouletteTaskQueueManager>()
			.AddHostedService<RouletteTaskBackgroundService>();

		return services;
	}

	public static IServiceCollection AddDepositAccumulateRouletteServiceClient(
		this IServiceCollection services,
		Action<ServiceClientOptions, IServiceProvider> configure)
		=> services.AddOptions<ServiceClientOptions>()
			.Configure(configure)
			.Services
			.AddHttpClient<IDepositAccumulateRouletteServiceClient, DepositAccumulateRouletteServiceClient>()
			.ConfigureHttpClient((sp, httpClient) =>
			{
				var options = sp.GetRequiredService<IOptions<ServiceClientOptions>>().Value;
				httpClient.BaseAddress = options.BaseUrl;
			})
			.Services;
}
