// ReSharper disable CheckNamespace

using System.Reflection;
using Microsoft.Extensions.Options;
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
			CachedGetDepositAccumulateRoulettePromotionQueryService>();

		return services;
	}

	public static IServiceCollection AddDepositAccumulateRouletteWorkerClient(
		this IServiceCollection services,
		Action<WorkerClientOptions, IServiceProvider> configure)
		=> services.AddOptions<WorkerClientOptions>()
			.Configure(configure)
			.Services
			.AddHttpClient<IDepositAccumulateRouletteWorkerClient, DepositAccumulateRouletteWorkerClient>()
			.ConfigureHttpClient((sp, httpClient) =>
			{
				var options = sp.GetRequiredService<IOptions<WorkerClientOptions>>().Value;
				httpClient.BaseAddress = options.BaseUrl;
			})
			.Services;
}
