using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;

namespace Tgs.Promotion.DepositAccumulateRouletteService.Services;

internal class RouletteHandleService : IRouletteHandleService
{
	private readonly IMessageSender _messageSender;

	public RouletteHandleService(
		IMessageSender messageSender)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public async ValueTask<RouletteResult> RequestAsync(
		RouletteRequest request,
		CancellationToken cancellationToken)
	{
		var taskRequest = new RouletteTaskRequest
		{
			PromotionId = request.PromotionId,
			PlayerCode = request.PlayerCode
		};

		var data = await _messageSender.RequestAsync(
			"tgs.promotion.depositaccumulateroulette.worker.roulette",
			taskRequest.ToByteArray(),
			cancellationToken)
			.ConfigureAwait(false);

		var response = RouletteTaskResponse.Parser.ParseFrom(data.Span);

		return new RouletteResult(response.Id, response.ItemIndex, response.RemainingCount);
	}
}
