using System.Runtime.CompilerServices;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class GetRouletteCountItemsQueryService
	: IQueryService<GetRouletteCountItemsQuery, RouletteCountItemInfo>
{
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly ITgsRepository _tgsRepository;
	private readonly IRouletteCountService _rouletteCountService;

	public GetRouletteCountItemsQueryService(
		IPlayerServiceClient playerServiceClient,
		ITgsRepository tgsRepository,
		IRouletteCountService rouletteCountService)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_rouletteCountService = rouletteCountService ?? throw new ArgumentNullException(nameof(rouletteCountService));
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
			var count = await _rouletteCountService
				.GetAvailableRouletteCountAsync(id, player.PlayerCode, cancellationToken)
				.ConfigureAwait(false);

			if (count > 0)
				yield return new RouletteCountItemInfo(id, count);
		}
	}
}
