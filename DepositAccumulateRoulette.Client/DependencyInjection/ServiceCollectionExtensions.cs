using Tgs.DepositAccumulateRoulette.Clients;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDepositAccumulateRouletteClient(this IServiceCollection services)
		=> services.AddTransient<IDepositAccumulateRouletteClient, DepositAccumulateRouletteClient>();
}
