using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.MessageQueue;

public class ClearExpiredDepositAccumulateEventCacheHandler(
	IStoreRepository storeRepository) : IMessageHandler<ReadOnlyMemory<byte>>
{
	private readonly IStoreRepository _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));

	public ValueTask HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = ClearEventCacheRequest.Parser.ParseFrom(data.Span);

		return _storeRepository
			.ClearExpiredDepositAccumulateRouletteEventsAsync(request.PromotionId, request.PlayerCode);
	}
}
