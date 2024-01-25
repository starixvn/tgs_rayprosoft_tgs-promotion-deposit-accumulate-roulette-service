using System.Runtime.CompilerServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class GetRouletteCountItemsQueryService(
	IPromotionService promotionService,
	IDepositAccumulateRouletteEventService eventService,
	ITgsRepository tgsRepository,
	IStoreRepository storeRepository)
		: IQueryService<GetRouletteCountItemsQuery, RouletteCountItemInfo>
{
	private readonly IPromotionService _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	private readonly IStoreRepository _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
	private readonly IDepositAccumulateRouletteEventService _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));

	public async IAsyncEnumerable<RouletteCountItemInfo> QueryAsync(
		GetRouletteCountItemsQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var promotionIds = _promotionService
			.GetPlayerAvailablePromotionIdsAsync(
				query.PlayerCode,
				cancellationToken);

		await foreach (var promotionId in promotionIds.WithCancellation(cancellationToken))
		{
			var infos = await _eventService
				.QueryDepositAccumulateRouletteEventInfosAsync(
					promotionId,
					query.PlayerCode,
					cancellationToken)
				.ToArrayAsync(cancellationToken)
				.ConfigureAwait(false);

			var totalCount = 0;

			foreach (var info in infos)
			{
				var usedCount = await GetRouletteUsedCountAsync(
						promotionId,
						query.PlayerCode,
						info,
						cancellationToken).ConfigureAwait(false);

				var availableCount = info.RouletteCount < usedCount ? 0 : info.RouletteCount - usedCount;

				totalCount += availableCount;
			}

			yield return new RouletteCountItemInfo(promotionId, totalCount);
		}
	}

	private async ValueTask<int> GetRouletteUsedCountAsync(
		Guid promotionId,
		string playerCode,
		DepositAccumulateRouletteEventInfo info,
		CancellationToken cancellationToken)
	{
		var keptUsedCount = await _storeRepository
			.GetRouletteUsedCountAsync(promotionId, playerCode, info.Date)
			.ConfigureAwait(false);

		if (keptUsedCount.HasValue)
			return (int)keptUsedCount;

		var usedCount = await _tgsRepository
			.GetRouletteUsedCountAsync(promotionId, playerCode, info.Date, cancellationToken)
			.ConfigureAwait(false);

		_ = await _storeRepository
			.SetRouletteUsedCountAsync(promotionId, playerCode, info.Date, usedCount)
			.ConfigureAwait(false);

		return usedCount;
	}
}
