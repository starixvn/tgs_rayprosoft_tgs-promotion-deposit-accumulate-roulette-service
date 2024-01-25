using Google.Protobuf;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

using Protos = Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.MessageQueue;

public class RouletteCountItemsQueryProcessor(
	IQueryService<GetRouletteCountItemsQuery, RouletteCountItemInfo> queryService) : IMessageProcessor<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>
{
	private readonly IQueryService<GetRouletteCountItemsQuery, RouletteCountItemInfo> _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = RouletteCountItemsQueryRequest.Parser.ParseFrom(data.Span);

		var rouletteCountItems = await _queryService
			.QueryAsync(new GetRouletteCountItemsQuery(request.PlayerCode), cancellationToken)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var items = rouletteCountItems.Select(p => new Protos.RouletteCountItemInfo
		{
			PromotionId = p.PromotionId,
			RouletteCount = p.RouletteCount
		});

		var response = new RouletteCountItemsQueryResponse();
		response.Items.AddRange(items);

		return response.ToByteArray();
	}
}
