using System.Text;
using System.Text.Json;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Core;
using Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

public class RouletteAvailableCountsQueryProcessor : IMessageProcessor
{
	private readonly IQueryService<GetPlayerAvailableRoulettePromotionsQuery, RoulettePromotionInfo> _queryService;

	public RouletteAvailableCountsQueryProcessor(
		IQueryService<GetPlayerAvailableRoulettePromotionsQuery, RoulettePromotionInfo> queryService)
	{
		_queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var playerCode = Encoding.UTF8.GetString(data.Span);

		var promotionInfos = await _queryService
			.QueryAsync(new GetPlayerAvailableRoulettePromotionsQuery(playerCode), cancellationToken)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var json = JsonSerializer.SerializeToUtf8Bytes(promotionInfos);

		return json;
	}
}
