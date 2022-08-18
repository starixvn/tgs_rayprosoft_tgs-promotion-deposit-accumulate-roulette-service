using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;
using Tgs.Promotion.DepositAccumulateRouletteServices;
using Tgs.Promotion.DepositAccumulateRouletteServices.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

public class RouletteCountItemsQueryProcessor : IMessageProcessor
{
	private readonly IQueryService<GetPlayerRouletteCountItemsQuery, RouletteCountItemInfo> _queryService;

	public RouletteCountItemsQueryProcessor(
		IQueryService<GetPlayerRouletteCountItemsQuery, RouletteCountItemInfo> queryService)
	{
		_queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = RouletteCountRequest.Parser.ParseFrom(data.Span);

		var promotionInfos = await _queryService
			.QueryAsync(new GetPlayerRouletteCountItemsQuery(request.PlayerCode), cancellationToken)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var promotions = promotionInfos.Select(p => new RouletteCountItem
		{
			PromotionId = p.PromotionId,
			RouletteCount = p.RouletteCount
		});

		var response = new RouletteCountResponse();
		response.Items.AddRange(promotions);

		return response.ToByteArray();
	}
}
