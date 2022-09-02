using System.Runtime.CompilerServices;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class GetRouletteCountItemsQueryService
	: IQueryService<GetRouletteCountItemsQuery, RouletteCountItemInfo>
{
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly ITgsRepository _tgsRepository;
	private readonly IRedisRepository _redisRepository;
	private readonly IDepositAccumulateRouletteEventService _eventService;

	public GetRouletteCountItemsQueryService(
		IPlayerServiceClient playerServiceClient,
		IDepositAccumulateRouletteEventService eventService,
		ITgsRepository tgsRepository,
		IRedisRepository redisRepository)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_redisRepository = redisRepository ?? throw new ArgumentNullException(nameof(redisRepository));
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
				var count = await GetRouletteCountAsync(promotionId, player.PlayerCode, info, cancellationToken)
					.ConfigureAwait(false);

				totalCount += count;
			}

			if (totalCount > 0)
				yield return new RouletteCountItemInfo(promotionId, totalCount);
		}
	}

	private async ValueTask<int> GetRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DepositAccumulateRouletteEventInfo info,
		CancellationToken cancellationToken)
	{
		var cachedCount = await _redisRepository
			.GetRouletteCountAsync(promotionId, playerCode, info.Date)
			.ConfigureAwait(false);

		if (cachedCount.HasValue)
			return (int)cachedCount;

		var usedCount = await _tgsRepository
			.GetRouletteUsedCountAsync(promotionId, playerCode, info.Date, cancellationToken)
			.ConfigureAwait(false);

		var count = info.RouletteCount >= usedCount ? info.RouletteCount - usedCount : 0;

		_ = await _redisRepository
			.SetRouletteCountAsync(promotionId, playerCode, info.Date, count)
			.ConfigureAwait(false);

		return count;
	}
}
