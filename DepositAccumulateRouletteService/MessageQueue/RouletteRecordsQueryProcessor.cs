using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

public class RouletteRecordsQueryProcessor : IMessageProcessor
{
	private readonly IMessageSender _messageSender;

	public RouletteRecordsQueryProcessor(
		IMessageSender messageSender)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
	{
		var request = RouletteRecordsRequest.Parser.ParseFrom(data.Span);

		var queryRequest = new RouletteRecordsQueryRequest
		{
			PlayerCode = request.PlayerCode,
			PromotionId = request.PromotionId
		};

		var responseData = await _messageSender
			.RequestAsync(
			"tgs.promotion.depositaccumulateroulette.worker.roulette.records.query",
			queryRequest.ToByteArray(),
			cancellationToken).ConfigureAwait(false);

		var queryResponse = RouletteRecordsQueryResponse.Parser.ParseFrom(responseData.Span);
		var rouletteRecords = queryResponse.RouletteRecords
			.Select(info => new RouletteRecord
			{
				Id = info.Id,
				Date = info.Date,
				ItemIndex = info.ItemIndex,
				CreatedOn = info.CreatedOn
			});

		var response = new RouletteRecordsResponse();
		response.RouletteRecords.AddRange(rouletteRecords);

		return response.ToByteArray();
	}
}
