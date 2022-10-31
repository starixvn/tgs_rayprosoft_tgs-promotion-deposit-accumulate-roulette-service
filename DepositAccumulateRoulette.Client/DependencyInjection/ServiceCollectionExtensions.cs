using Microsoft.Extensions.Options;
using Tgs.DepositAccumulateRoulette.Clients;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDepositAccumulateRouletteClient(
			this IServiceCollection services,
		Action<DepositAccumulateRouletteOptions, IServiceProvider> configure)
		=> services
			.AddOptions<DepositAccumulateRouletteOptions>()
			.Configure(configure)
			.Services
			.AddHttpClient<IDepositAccumulateRouletteServiceClient, DepositAccumulateRouletteServiceClient>()
			.ConfigureHttpClient((sp, httpClient) =>
			{
				var options = sp.GetRequiredService<IOptions<DepositAccumulateRouletteOptions>>().Value;
				httpClient.BaseAddress = options.BaseUrl;
			})

			.Services
			.AddTransient<IDepositAccumulateRouletteClient, DepositAccumulateRouletteClient>();
}
