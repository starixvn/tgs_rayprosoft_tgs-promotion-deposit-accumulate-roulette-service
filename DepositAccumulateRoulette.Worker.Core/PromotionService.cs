using System.Runtime.CompilerServices;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class PromotionService : IPromotionService
{
	private readonly ISystemClock _systemClock;
	private readonly ITgsRepository _tgsRepository;
	private readonly IStoreRepository _storeRepository;
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;

	public PromotionService(
		ISystemClock systemClock,
		ITgsRepository tgsRepository,
		IStoreRepository storeRepository,
		IPromotionServiceClient promotionServiceClient,
		IPlayerServiceClient playerServiceClient)
	{
		_systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
	}

	public async IAsyncEnumerable<Guid> GetPlayerAvailablePromotionIdsAsync(
		string playerCode,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var now = _systemClock.UtcNow.ToOffset(TimeSpan.FromHours(8D));
		var currentDate = DateOnly.FromDateTime(now.DateTime);

		// 查詢日期: 以每日 8:00 UTC+8 為準
		var queryDate = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(8 - 8)).DateTime);

		var events = await _storeRepository
			.GetPlayerAvailablePromotionIdsAsync(playerCode, queryDate)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		if (events.Length > 0)
		{
			foreach (var evt in events)
				yield return evt;
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

			// expire time 計算:
			// 1. 排程起始時間: 每日 07:00 UTC+8, 預期排程最多執行時間為 1 小時
			// 2. 查詢日期: 以每日 8:00 UTC+8 為準
			// 3. 所有的 promotion id 以 json 存放於 redis string, 若查詢時間與當日日期相同, 資料保存至隔日 07:00 UTC+8
			// 4. 若查詢時間與當日日期不同 (發生於排程時間中), 則資料保存時間為 5 分鐘
			var expiry = TimeSpan.FromMinutes(5);

			if (currentDate == queryDate)
			{
				var nextDate = currentDate.AddDays(1);

				expiry = new DateTimeOffset(nextDate.Year, nextDate.Month, nextDate.Day, 7, 0, 0, TimeSpan.FromHours(8D)) - now;
			}

			await _storeRepository
				.SetPlayerAvailablePromotionIdsAsync(
					playerCode, queryDate, allPromotionIds, expiry)
				.ConfigureAwait(false);

			foreach (var evt in allPromotionIds)
				yield return evt;
		}
	}
}
