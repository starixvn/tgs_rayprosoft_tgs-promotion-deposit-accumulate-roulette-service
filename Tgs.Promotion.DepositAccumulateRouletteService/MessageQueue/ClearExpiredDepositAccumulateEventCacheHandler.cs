using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

public class ClearExpiredDepositAccumulateEventCacheHandler : IMessageHandler
{
	//private readonly IDepositAccumulateRouletteEventHandler _eventHandler;
	//private readonly ITgsRepository _tgsRepository;

	//public ClearExpiredDepositAccumulateEventCacheHandler(
	//	IDepositAccumulateRouletteEventHandler eventHandler,
	//	ITgsRepository tgsRepository)
	//{
	//	_eventHandler = eventHandler ?? throw new ArgumentNullException(nameof(eventHandler));
	//	_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	//}

	//public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	//{
	//	var request = ClearEventCacheRequest.Parser.ParseFrom(data.Span);

	//	return _eventHandler.ClearExpiredEventsAsync(request.PromotionId, request.PlayerCode);
	//}
	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
		=> throw new NotImplementedException();
}
