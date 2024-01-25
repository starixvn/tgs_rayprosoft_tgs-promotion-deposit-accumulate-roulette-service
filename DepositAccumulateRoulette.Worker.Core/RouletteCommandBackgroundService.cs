using Microsoft.Extensions.Hosting;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class RouletteCommandBackgroundService(RouletteCommandQueueManager rouletteCommandQueueManager) : BackgroundService
{
	private readonly RouletteCommandQueueManager _rouletteCommandQueueManager = rouletteCommandQueueManager ?? throw new ArgumentNullException(nameof(rouletteCommandQueueManager));

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_ = stoppingToken.Register(() => _rouletteCommandQueueManager.StopReceive());

		return Task.Run(async () =>
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				_rouletteCommandQueueManager.ClearExpiredQueues();

				await Task.Delay(10000, stoppingToken).ConfigureAwait(false);
			}
		}, stoppingToken);
	}
}
