using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tgs.Promotion.DepositAccumulateRoulette.Repositories.Entities;

namespace Tgs.Promotion.DepositAccumulateRouletteService.IntegrationTests;

extern alias RouletteSvc;

internal class TestDepositAccumulateRouletteServiceApplication : WebApplicationFactory<RouletteSvc.Program>
{
	private readonly Action<IWebHostBuilder>? _webHostConfiguration;

	public TestDepositAccumulateRouletteServiceApplication(
		Action<IWebHostBuilder>? webHostConfiguration = null) => _webHostConfiguration = webHostConfiguration;

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		_ = builder.ConfigureAppConfiguration(configurationBuilder => configurationBuilder.Sources.Clear());

		_ = builder.ConfigureServices(services => services
			.AddFakeNatsMessageQueue()
			.Replace(ServiceDescriptor.Singleton(sp =>
			{
				var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsContext>();

				_ = dbContextOptionsBuilder
					.UseInMemoryDatabase(Guid.NewGuid().ToString())
					.ConfigureWarnings(x => x.Ignore(
						InMemoryEventId.TransactionIgnoredWarning));

				return new TgsContext(dbContextOptionsBuilder.Options);
			})));

		_webHostConfiguration?.Invoke(builder);
	}
}
