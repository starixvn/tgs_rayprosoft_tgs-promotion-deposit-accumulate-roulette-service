using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class RouletteTaskCommandService : IGetService<RouletteTaskCommand, RouletteResult>
{
	private readonly RouletteTaskQueueManager _queueManager;

	public RouletteTaskCommandService(
		RouletteTaskQueueManager queueManager)
	{
		_queueManager = queueManager ?? throw new ArgumentNullException(nameof(queueManager));
	}

	public async ValueTask<RouletteResult> GetAsync(
		RouletteTaskCommand command,
		CancellationToken cancellationToken = default)
	{
		var request = new RouletteTaskQueueRequest(command.PromotionId, command.PlayerCode);

		return await _queueManager.RequestAsync(request).ConfigureAwait(false);
	}
}
