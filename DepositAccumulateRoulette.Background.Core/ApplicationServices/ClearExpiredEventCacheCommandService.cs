using Google.Protobuf;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.ApplicationServices;

internal class ClearExpiredEventCacheCommandService : ICommandService<ClearExpiredEventCacheCommand>
{
	private readonly IMessageSender _messageSender;

	public ClearExpiredEventCacheCommandService(
		IMessageSender messageSender) => _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));

	public async ValueTask ExecuteAsync(
		ClearExpiredEventCacheCommand command,
		CancellationToken cancellationToken = default)
	{
		var subject = "tgs.promotion.depositaccumulateroulette.event.expired.clear";

		foreach (var info in command.Infos)
		{
			var request = new ClearEventCacheRequest
			{
				PromotionId = info.PromotionId,
				PlayerCode = info.PlayerCode
			};

			await _messageSender.PublishAsync(subject, request.ToByteArray(), cancellationToken)
				.ConfigureAwait(false);
		}
	}
}
