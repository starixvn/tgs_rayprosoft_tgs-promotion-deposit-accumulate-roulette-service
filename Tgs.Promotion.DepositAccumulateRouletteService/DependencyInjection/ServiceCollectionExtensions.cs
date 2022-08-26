// ReSharper disable CheckNamespace

using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRouletteService;
using Tgs.Promotion.DepositAccumulateRouletteService.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDepositAccumulateRouletteService(
		this IServiceCollection services,
		Func<IServiceProvider, IDatabase> resolveRedisDatabase)
		=> services.AddTransient<IRouletteHandleService>(sp =>
		{
			var database = resolveRedisDatabase(sp);

			return ActivatorUtilities.CreateInstance<RouletteHandleService>(sp, database);
		});
}
