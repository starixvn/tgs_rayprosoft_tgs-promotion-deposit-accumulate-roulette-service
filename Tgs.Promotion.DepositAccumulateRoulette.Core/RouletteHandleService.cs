using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRouletteServices;

internal class RouletteHandleService : IRouletteHandleService
{
	private readonly IMessageSender _messageSender;

	public RouletteHandleService(
		IMessageSender messageSender)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public async ValueTask<RouletteResult> RequestAsync(
		RouletteSessionRequest request,
		CancellationToken cancellationToken)
	{
		var answer = await _messageSender.AskAsync(
			"tgs.promotion.depositaccumulateroulette.worker",
			request.ToByteArray(),
			cancellationToken)
			.ConfigureAwait(false);

		var response = RouletteSessionResponse.Parser.ParseFrom(answer.Result.Span);

		await answer
			.CompleteAsync(Array.Empty<byte>(), cancellationToken)
			.ConfigureAwait(false);

		return new RouletteResult(response.Id, response.ItemIndex, response.RemainingCount);
	}
}
