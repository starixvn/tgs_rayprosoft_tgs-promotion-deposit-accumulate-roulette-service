using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDepositAccumulateRouletteCore(
		this IServiceCollection services,
		Action<PromotionServiceOptions, IServiceProvider> configure)
	{
		_ = services
			.AddOptions<PromotionServiceOptions>()
			.Configure(configure)
			.Services
			.AddSingleton(sp =>
			{
				var options = sp.GetRequiredService<IOptions<PromotionServiceOptions>>().Value;

				return new RedisConnectionManager(options.RedisConnectionString);
			})
			.RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient);

		_ = services
			.AddSingleton<IDistributePlanHasher, DistributePlanHasher>()
			.AddSingleton<IStatisticsService, StatisticsService>()
			.AddSingleton<IShuffleService, ShuffleService>()
			.Decorate<IShuffleService, MemoryCachedShuffleService>();

		_ = services
			.Decorate<IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>, MemoryCachedGetPromotionDistributePlanQueryService>();

		_ = services
			//.AddTransient<
			//	IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo>,
			//	GetDepositAccumulateEventsQueryService>()
			//.Decorate<IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo>, RedisCachedGetDepositAccumulateEventQueryService>()
			.Decorate<IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo>>(
				(inner, sp) =>
				{
					var redisCacheManager = sp.GetRequiredService<RedisConnectionManager>();

					return new RedisCachedGetDepositAccumulateEventQueryService(inner, redisCacheManager.Database);
				})
			.AddTransient<IClearDepositAccumulateRouletteEventCache, RedisCachedGetDepositAccumulateEventQueryService>()
			.AddTransient<IRouletteCountService>(sp =>
			{
				var redisConnectionManager = sp.GetRequiredService<RedisConnectionManager>();

				return ActivatorUtilities.CreateInstance<RouletteCountService>(sp,
					redisConnectionManager.Database);
			});

		return services;
	}
}
