using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.DepositAccumulateRoulette.Background;
using Tgs.Promotion.DepositAccumulateRoulette.Background.Repositories;
using Tgs.Promotion.DepositAccumulateRoulette.Background.Repositories.Entities;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDepositAccumulateRouletteRepositories(
		this IServiceCollection services,
		Action<IServiceProvider, DbContextOptionsBuilder>? dbOptionsAction = null)
		=> services
			.AddDbContext<TgsContext>(dbOptionsAction)
			.AddTransient<IBackgroundRepository, BackgroundRepository>();
}
