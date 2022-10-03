using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

public class RouletteCountItemsQueryProcessor : IMessageProcessor
{
	private readonly IMessageSender _messageSender;

	public RouletteCountItemsQueryProcessor(
		IMessageSender messageSender)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
	{
		var request = RouletteCountRequest.Parser.ParseFrom(data.Span);

		var queryRequest = new RouletteCountItemsQueryRequest
		{
			PlayerCode = request.PlayerCode
		};

		var responseData = await _messageSender
			.RequestAsync(
				"tgs.promotion.depositaccumulateroulette.worker.roulettecountitems",
				queryRequest.ToByteArray(),
				cancellationToken)
			.ConfigureAwait(false);

		var queryResponse = RouletteCountItemsQueryResponse.Parser.ParseFrom(responseData.Span);

		var items = queryResponse.Items.Select(item => new RouletteCountItem
		{
			PromotionId = item.PromotionId,
			RouletteCount = item.RouletteCount
		});

		var response = new RouletteCountResponse();
		response.Items.AddRange(items);

		return response.ToByteArray();
	}
}
