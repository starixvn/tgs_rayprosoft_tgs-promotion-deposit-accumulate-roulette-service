using System.Runtime.CompilerServices;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class PromotionService(
	ISystemClock systemClock,
	ITgsRepository tgsRepository,
	IStoreRepository storeRepository,
	IPromotionServiceClient promotionServiceClient,
	IPlayerServiceClient playerServiceClient) : IPromotionService
{
	private readonly ISystemClock _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	private readonly IStoreRepository _storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
	private readonly IPromotionServiceClient _promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));

	public async IAsyncEnumerable<Guid> GetPlayerAvailablePromotionIdsAsync(
		string playerCode,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var now = _systemClock.UtcNow.ToOffset(TimeSpan.FromHours(8D));

		// 查詢日期: 以每日 8:00 UTC+8 為準
		var queryDate = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(8 - 8)).DateTime);

		var storedIds = await _storeRepository
			.GetPlayerAvailablePromotionIdsAsync(playerCode, queryDate)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		if (storedIds.Length > 0)
		{
			foreach (var id in storedIds)
				yield return id;
		}
		else
		{
			var playerInfo = (await _playerServiceClient.GetPlayerInfoAsync(playerCode, cancellationToken)
				.ConfigureAwait(false))!;

			var validPromotionIds = await _promotionServiceClient
				.ListPromotionsAsync(playerInfo.BrandId, playerCode, cancellationToken)
				.Where(p => p.PromotionType == "DepositAccumulateRoulette")
				.Select(p => p.Id)
				.ToArrayAsync(cancellationToken)
				.ConfigureAwait(false);

			var availablePromotionIds = await _tgsRepository
				.GetPlayerRouletteAvailablePromotionIdsAsync(
					playerCode,
					queryDate,
					cancellationToken)
				.ToArrayAsync(cancellationToken)
				.ConfigureAwait(false);

			var allPromotionIds = validPromotionIds
				.Concat(availablePromotionIds)
				.Distinct()
				.ToArray();

			var expiry = TimeSpan.FromMinutes(5);

			await _storeRepository
				.SetPlayerAvailablePromotionIdsAsync(
					playerCode, queryDate, allPromotionIds, expiry)
				.ConfigureAwait(false);

			foreach (var id in allPromotionIds)
				yield return id;
		}
	}
}
