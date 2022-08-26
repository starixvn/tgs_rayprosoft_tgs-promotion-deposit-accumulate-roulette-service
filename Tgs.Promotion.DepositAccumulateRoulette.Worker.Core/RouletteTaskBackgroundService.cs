using Microsoft.Extensions.Hosting;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class RouletteTaskBackgroundService : BackgroundService
{
	private readonly RouletteTaskQueueManager _rouletteTaskQueueManager;

	public RouletteTaskBackgroundService(RouletteTaskQueueManager rouletteTaskQueueManager)
	{
		_rouletteTaskQueueManager = rouletteTaskQueueManager ?? throw new ArgumentNullException(nameof(rouletteTaskQueueManager));
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_ = stoppingToken.Register(() => _rouletteTaskQueueManager.StopReceive());

		return Task.CompletedTask;
	}
}
