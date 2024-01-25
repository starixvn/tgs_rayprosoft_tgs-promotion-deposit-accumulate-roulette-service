using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDepositAccumulateRouletteCommon(
		this IServiceCollection services)
		=> services
			.AddSingleton<DepositAccumulateRouletteSerializer>();
}