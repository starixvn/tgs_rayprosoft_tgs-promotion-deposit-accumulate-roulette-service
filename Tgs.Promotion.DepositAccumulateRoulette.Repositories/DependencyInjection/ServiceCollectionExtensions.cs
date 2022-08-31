using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Repositories;
using Tgs.Promotion.DepositAccumulateRoulette.Repositories.Entities;
using Tgs.Promotion.DepositAccumulateRoulette.Worker;

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
			.AddTransient<IRedisRepository>(sp =>
			{
				var redisDatabase = resolveRedisDatabase(sp);

				return ActivatorUtilities.CreateInstance<RedisRepository>(sp, redisDatabase);
			})
			.AddSerialNumberGenerator();
}
