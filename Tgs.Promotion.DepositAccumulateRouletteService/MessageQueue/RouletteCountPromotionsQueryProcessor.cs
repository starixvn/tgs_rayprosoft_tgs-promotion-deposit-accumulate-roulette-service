using System.Data;
using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;
using Tgs.Promotion.DepositAccumulateRouletteServices;
using Tgs.Promotion.DepositAccumulateRouletteServices.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

public class RouletteCountPromotionsQueryProcessor : IMessageProcessor
{
	private readonly IQueryService<GetPlayerRouletteCountPromotionsQuery, RouletteCountPromotionInfo> _queryService;

	public RouletteCountPromotionsQueryProcessor(
		IQueryService<GetPlayerRouletteCountPromotionsQuery, RouletteCountPromotionInfo> queryService)
	{
		_queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = RouletteCountPromotionsReqest.Parser.ParseFrom(data.Span);

		var promotionInfos = await _queryService
			.QueryAsync(new GetPlayerRouletteCountPromotionsQuery(request.PlayerCode), cancellationToken)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var promotions = promotionInfos.Select(p => new RouletteCountPromotion
		{
			PromotionId = p.PromotionId,
			RouletteCount = p.RouletteCount
		});

		var response = new RouletteCountPromotionsResponse();
		response.Promotions.AddRange(promotions);

		return response.ToByteArray();
	}
}
