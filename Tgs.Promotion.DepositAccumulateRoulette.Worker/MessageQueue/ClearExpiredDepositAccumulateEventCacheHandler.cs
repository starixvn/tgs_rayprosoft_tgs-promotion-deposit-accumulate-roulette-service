using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.MessageQueue;

public class ClearExpiredDepositAccumulateEventCacheHandler : IMessageHandler
{
	private readonly IRedisRepository _redisRepository;

	public ClearExpiredDepositAccumulateEventCacheHandler(
		IRedisRepository redisRepository)
	{
		_redisRepository = redisRepository ?? throw new ArgumentNullException(nameof(redisRepository));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = ClearEventCacheRequest.Parser.ParseFrom(data.Span);

		return _redisRepository
			.ClearExpiredDepositAccumulateRouletteEventsAsync(request.PromotionId, request.PlayerCode);
	}
}
