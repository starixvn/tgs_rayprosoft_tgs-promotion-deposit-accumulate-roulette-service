using System.Runtime.CompilerServices;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class GetRouletteCountItemsQueryService
	: IQueryService<GetRouletteCountItemsQuery, RouletteCountItemInfo>
{
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly ITgsRepository _tgsRepository;
	private readonly IQueryService<GetRouletteEventCountQuery, RouletteEventCountInfo> _eventCountQueryService;

	public GetRouletteCountItemsQueryService(
		IPlayerServiceClient playerServiceClient,
		ITgsRepository tgsRepository,
		IQueryService<GetRouletteEventCountQuery, RouletteEventCountInfo> eventCountQueryService)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_eventCountQueryService = eventCountQueryService ?? throw new ArgumentNullException(nameof(eventCountQueryService));
	}

	public async IAsyncEnumerable<RouletteCountItemInfo> QueryAsync(
		GetRouletteCountItemsQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var player = await _playerServiceClient.GetPlayerInfoAsync(query.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		if (player is null)
			throw new InvalidOperationException($"Can't find player with code {query.PlayerCode}");

		var promotionIds = await _tgsRepository.GetAvailablePromotionIdsAsync(
				player.BrandId, player.PlayerCode, cancellationToken)
			.ToArrayAsync(cancellationToken).ConfigureAwait(false);

		foreach (var id in promotionIds)
		{
			var infos = await _eventCountQueryService.QueryAsync(
					new GetRouletteEventCountQuery(id, query.PlayerCode),
					cancellationToken)
				.ToArrayAsync(cancellationToken)
				.ConfigureAwait(false);

			var totalCount = infos.Sum(info => info.AvailableCount);

			if (totalCount > 0)
				yield return new RouletteCountItemInfo(id, totalCount);
		}
	}
}
