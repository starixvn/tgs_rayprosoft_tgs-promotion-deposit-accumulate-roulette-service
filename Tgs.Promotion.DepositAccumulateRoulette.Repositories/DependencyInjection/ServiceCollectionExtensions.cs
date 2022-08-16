using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Repositories;
using Tgs.Promotion.DepositAccumulateRoulette.Repositories.Entities;
using Tgs.Promotion.DepositAccumulateRouletteServices;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDepositAccumulateRouletteRepositories(
		this IServiceCollection services,
		Action<IServiceProvider, DbContextOptionsBuilder>? dbOptionsConfigure,
		Func<IServiceProvider, IDatabase> resolveRedisDatabase)
		=> services
			.AddDbContext<TgsContext>(dbOptionsConfigure)
			.AddTransient<ITgsRepository, TgsRepository>()
			.AddSingleton<ISystemClock, SystemClock>()
			.AddSingleton<IStatisticsRepository>(sp =>
			{
				var redisDatabase = resolveRedisDatabase(sp);

				return ActivatorUtilities.CreateInstance<StatisticsRepository>(sp, redisDatabase);
			})
			.AddTransient<IDepositAccumulateRouletteEventRepository>(
				sp =>
				{
					var redisDatabase = resolveRedisDatabase(sp);

					return ActivatorUtilities.CreateInstance<DepositAccumulateRouletteEventRepository>(sp, redisDatabase);
				})
			.AddTransient<IDepositAccumulateRouletteEventHandler>(sp =>
			{
				var redisDatabase = resolveRedisDatabase(sp);

				return ActivatorUtilities.CreateInstance<DepositAccumulateRouletteEventRepository>(sp, redisDatabase);
			})
			.AddTransient<IRouletteCountRepository>(sp =>
			{
				var redisDatabase = resolveRedisDatabase(sp);

				return ActivatorUtilities.CreateInstance<RouletteCountRepository>(sp, redisDatabase);
			})
			.AddSerialNumberGenerator();
}
