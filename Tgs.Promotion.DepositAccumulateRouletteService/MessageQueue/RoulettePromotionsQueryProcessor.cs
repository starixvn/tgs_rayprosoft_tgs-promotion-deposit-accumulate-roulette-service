using System.Data;
using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;
using Tgs.Promotion.DepositAccumulateRoulette.Core;
using Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

public class RoulettePromotionsQueryProcessor : IMessageProcessor
{
	private readonly IQueryService<GetPlayerAvailableRoulettePromotionsQuery, RoulettePromotionInfo> _queryService;

	public RoulettePromotionsQueryProcessor(
		IQueryService<GetPlayerAvailableRoulettePromotionsQuery, RoulettePromotionInfo> queryService)
	{
		_queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = RoulettePromotionsReqest.Parser.ParseFrom(data.Span);

		var promotionInfos = await _queryService
			.QueryAsync(new GetPlayerAvailableRoulettePromotionsQuery(request.PlayerCode), cancellationToken)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var promotions = promotionInfos.Select(p => new RoulettePromotion
		{
			PromotionId = p.PromotionId,
			RouletteCount = p.RouletteCount
		});

		var response = new RoulettePromotionsResponse();
		response.Promotions.AddRange(promotions);

		return response.ToByteArray();
	}
}
