using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.MessageQueue;

public class ClearExpiredDepositAccumulateEventCacheHandler : IMessageHandler
{
	private readonly IStoreRepository _storeRepository;

	public ClearExpiredDepositAccumulateEventCacheHandler(
		IStoreRepository storeRepository)
	{
		_storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = ClearEventCacheRequest.Parser.ParseFrom(data.Span);

		return _storeRepository
			.ClearExpiredDepositAccumulateRouletteEventsAsync(request.PromotionId, request.PlayerCode);
	}
}
