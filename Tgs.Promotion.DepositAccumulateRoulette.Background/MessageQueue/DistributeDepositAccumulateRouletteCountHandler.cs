using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Background.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRoulette.Background.Protos;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.MessageQueue;

internal class DistributeDepositAccumulateRouletteCountHandler : IMessageHandler
{
	private readonly IQueryService<DistributeDepositAccumulateRouletteCountCommand, DepositAccumulateRouletteEventAddInfo> _distributeCommandService;
	private readonly ICommandService<ClearExpiredEventCacheCommand> _clearCacheCommandService;

	public DistributeDepositAccumulateRouletteCountHandler(
		IQueryService<DistributeDepositAccumulateRouletteCountCommand, DepositAccumulateRouletteEventAddInfo> distributeCommandService,
		ICommandService<ClearExpiredEventCacheCommand> clearCacheCommandService)
	{
		_distributeCommandService = distributeCommandService ?? throw new ArgumentNullException(nameof(distributeCommandService));
		_clearCacheCommandService = clearCacheCommandService ?? throw new ArgumentNullException(nameof(clearCacheCommandService));
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = ProcessRequest.Parser.ParseFrom(data.Span);

		var requestDate = request.ProcessDate.ToDateOnly();

		if (requestDate is null)
			return;

		var command = new DistributeDepositAccumulateRouletteCountCommand(requestDate.Value);

		var distributeEvents = await _distributeCommandService
			.QueryAsync(command, cancellationToken)
			.ToArrayAsync(cancellationToken);

		var clearCacheInfos = distributeEvents.Select(evt =>
				new ClearCacheInfo(evt.PromotionId, evt.PlayerCode))
			.Distinct();

		await _clearCacheCommandService.ExecuteAsync(
			new ClearExpiredEventCacheCommand(clearCacheInfos),
			cancellationToken)
			.ConfigureAwait(false);
	}
}
