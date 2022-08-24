using Microsoft.Extensions.Hosting;

namespace Tgs.Promotion.DepositAccumulateRouletteServices;

internal class RouletteTaskBackgroundService : BackgroundService
{
	private readonly RouletteTaskProcessor _rouletteTaskProcessor;

	public RouletteTaskBackgroundService(RouletteTaskProcessor rouletteTaskProcessor)
	{
		_rouletteTaskProcessor = rouletteTaskProcessor ?? throw new ArgumentNullException(nameof(rouletteTaskProcessor));
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_ = stoppingToken.Register(() => _rouletteTaskProcessor.StopReceive());

		return Task.CompletedTask;
	}
}
