using Tgs.Promotion.DepositAccumulateRoulette.Background.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRoulette.Background.Protos;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.MessageQueue;

internal class DistributeDepositAccumulateRouletteCountHandler : IMessageHandler<ReadOnlyMemory<byte>>
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

	public async ValueTask HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = ProcessRequest.Parser.ParseFrom(data.Span);

		var requestDate = request.ProcessDate.ToDateOnly();

		if (requestDate is null)
			return;

		var command = new DistributeDepositAccumulateRouletteCountCommand(requestDate.Value);

		var distributeEvents = _distributeCommandService
			.QueryAsync(command, cancellationToken);

		var clearCacheInfos = await distributeEvents
			.Select(evt => new ClearCacheInfo(evt.PromotionId, evt.PlayerCode))
			.Distinct()
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		await _clearCacheCommandService.ExecuteAsync(
			new ClearExpiredEventCacheCommand(clearCacheInfos),
			cancellationToken).ConfigureAwait(false);
	}
}
