using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class RouletteTaskCommandService : IGetService<RouletteTaskCommand, RouletteResult>
{
	private readonly RouletteCommandQueueManager _queueManager;

	public RouletteTaskCommandService(
		RouletteCommandQueueManager queueManager) => _queueManager = queueManager ?? throw new ArgumentNullException(nameof(queueManager));

	public async ValueTask<RouletteResult> GetAsync(
		RouletteTaskCommand command,
		CancellationToken cancellationToken = default)
	{
		var request = new RouletteCommandRequest(command.PromotionId, command.PlayerCode);

		return await _queueManager.RequestAsync(request).ConfigureAwait(false);
	}
}
