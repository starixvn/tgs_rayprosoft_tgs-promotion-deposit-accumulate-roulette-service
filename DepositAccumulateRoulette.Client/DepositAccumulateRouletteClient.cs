using Google.Protobuf;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;
using Valhalla.MessageQueue;

namespace Tgs.DepositAccumulateRoulette.Clients;

internal class DepositAccumulateRouletteClient : IDepositAccumulateRouletteClient
{
	private readonly IMessageSender _messageSender;

	public DepositAccumulateRouletteClient(
		IMessageSender messageSender) => _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));

	public async ValueTask<RouletteResult> RouletteAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default)
	{
		var request = new RouletteRequest
		{
			PromotionId = promotionId,
			PlayerCode = playerCode
		};

		var responseData = await _messageSender.RequestAsync<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(
				"tgs.promotion.depositaccumulateroulette.roulette.play",
				request.ToByteArray(),
				cancellationToken)
			.ConfigureAwait(false);

		var response = RouletteResponse.Parser.ParseFrom(responseData.Span);

		return new RouletteResult(
			response.Id,
			response.ItemIndex);
	}
}
