using Google.Protobuf;
using Tgs.Promotion.Clients.Protos;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

public class RouletteCountItemsQueryProcessor : IMessageProcessor<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>
{
	private readonly IMessageSender _messageSender;

	public RouletteCountItemsQueryProcessor(
		IMessageSender messageSender) => _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = RouletteCountRequest.Parser.ParseFrom(data.Span);

		var queryRequest = new RouletteCountItemsQueryRequest
		{
			PlayerCode = request.PlayerCode
		};

		var responseData = await _messageSender
			.RequestAsync<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(
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
