using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;

namespace Tgs.DepositAccumulateRoulette.Clients;

internal class DepositAccumulateRouletteClient : IDepositAccumulateRouletteClient
{
	private readonly IMessageSender _messageSender;

	public DepositAccumulateRouletteClient(
		IMessageSender messageSender)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public async ValueTask<RouletteSetting?> GetRouletteSettingAsync(Guid promotionId, CancellationToken cancellationToken = default)
	{
		var request = new RouletteSettingQueryRequest
		{
			PromotionId = promotionId
		};

		var responseData = await _messageSender.RequestAsync(
			"tgs.promotion.depositaccumulateroulette.roulettesetting.query",
			request.ToByteArray(),
			cancellationToken)
			.ConfigureAwait(false);

		var response = RouletteSettingQueryResponse.Parser.ParseFrom(responseData.Span);

		if (response?.PromotionId is null)
			return null;

		var items = response.Items.Select(item => new RouletteItem(item.Id, item.Title));

		return new RouletteSetting(response.PromotionId, items);
	}

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

		var responseData = await _messageSender.RequestAsync(
				"tgs.promotion.depositaccumulateroulette.roulette.play",
				request.ToByteArray(),
				cancellationToken)
			.ConfigureAwait(false);

		var response = RouletteResponse.Parser.ParseFrom(responseData.Span);

		return new RouletteResult(
			response.Id,
			response.ItemIndex,
			response.RemainingCount);
	}
}
