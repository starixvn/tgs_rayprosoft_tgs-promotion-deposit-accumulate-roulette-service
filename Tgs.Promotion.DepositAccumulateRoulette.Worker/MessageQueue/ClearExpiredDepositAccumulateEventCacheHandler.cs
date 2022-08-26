using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.MessageQueue;

public class ClearExpiredDepositAccumulateEventCacheHandler : IMessageHandler
{
	private readonly IDepositAccumulateRouletteEventHandler _eventHandler;

	public ClearExpiredDepositAccumulateEventCacheHandler(
		IDepositAccumulateRouletteEventHandler eventHandler)
	{
		_eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = ClearEventCacheRequest.Parser.ParseFrom(data.Span);

		return _eventHandler.ClearExpiredEventsAsync(request.PromotionId, request.PlayerCode);
	}
}
