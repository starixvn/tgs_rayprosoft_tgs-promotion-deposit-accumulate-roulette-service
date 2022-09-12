using System.Runtime.CompilerServices;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class GetRouletteCountItemsQueryService
	: IQueryService<GetRouletteCountItemsQuery, RouletteCountItemInfo>
{
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly ITgsRepository _tgsRepository;
	private readonly IStoreRepository _storeRepository;
	private readonly IDepositAccumulateRouletteEventService _eventService;

	public GetRouletteCountItemsQueryService(
		IPlayerServiceClient playerServiceClient,
		IDepositAccumulateRouletteEventService eventService,
		ITgsRepository tgsRepository,
		IStoreRepository storeRepository)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
		_eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
	}

	public async IAsyncEnumerable<RouletteCountItemInfo> QueryAsync(
		GetRouletteCountItemsQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var player = await _playerServiceClient.GetPlayerInfoAsync(query.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		if (player is null)
			throw new InvalidOperationException($"Can't find player with code {query.PlayerCode}");

		var promotionIds = await _tgsRepository
			.GetAvailablePromotionIdsAsync(
				player.PlayerCode,
				cancellationToken)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		foreach (var promotionId in promotionIds)
		{
			var infos = await _eventService
				.GetDepositAccumulateRouletteEventInfosAsync(
					promotionId,
					query.PlayerCode,
					cancellationToken)
				.ToArrayAsync(cancellationToken)
				.ConfigureAwait(false);

			var totalCount = 0;

			foreach (var info in infos)
			{
				var usedCount = await GetRouletteUsedCountAsync(promotionId, player.PlayerCode, info, cancellationToken)
					.ConfigureAwait(false);

				var availableCount = info.RouletteCount < usedCount ? 0 : info.RouletteCount - usedCount;

				totalCount += availableCount;
			}

			if (totalCount > 0)
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
