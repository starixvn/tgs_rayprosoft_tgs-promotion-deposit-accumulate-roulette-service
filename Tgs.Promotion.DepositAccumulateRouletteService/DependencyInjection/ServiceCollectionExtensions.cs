// ReSharper disable CheckNamespace

using Tgs.Promotion.DepositAccumulateRouletteService;
using Tgs.Promotion.DepositAccumulateRouletteService.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDepositAccumulateRouletteService(
		this IServiceCollection services)
		=> services.AddTransient<IRouletteHandleService, RouletteHandleService>();
}
