using System.Runtime.CompilerServices;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class DepositAccumulateRouletteEventService : IDepositAccumulateRouletteEventService
{
	private readonly ISystemClock _systemClock;
	private readonly ITgsRepository _tgsRepository;
	private readonly IStoreRepository _storeRepository;

	public DepositAccumulateRouletteEventService(
		ISystemClock systemClock,
		ITgsRepository tgsRepository,
		IStoreRepository storeRepository)
	{
		_systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
	}

	public async IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var now = _systemClock.UtcNow.ToOffset(TimeSpan.FromHours(8D));
		var currentDate = DateOnly.FromDateTime(now.DateTime);

		// assume the query date is 08:00 UTC+8
		var queryDate = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(8 - 8)).DateTime);

		var events = await _storeRepository
			.GetDepositAccumulateRouletteEventInfosAsync(promotionId, playerCode, now)
			.ToArrayAsync(cancellationToken);

		if (events.Length > 0)
		{
			foreach (var evt in events)
				yield return evt;
		}
		else
		{
			var eventInfos = QueryDepositAccumulateRouletteEventInfosAsync(
				promotionId,
				playerCode,
				queryDate,
				currentDate,
				now,
				cancellationToken);

			await foreach (var evt in eventInfos.WithCancellation(cancellationToken))
				yield return evt;
		}
	}

	private async IAsyncEnumerable<DepositAccumulateRouletteEventInfo> QueryDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		DateOnly queryDate,
		DateOnly currentDate,
		DateTimeOffset now,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var events = await _tgsRepository
			.GetDepositAccumulateRouletteEventInfosAsync(
				promotionId,
				playerCode,
				queryDate)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		// expire time 計算:
		// 1. 排程起始時間: 每日 07:00 UTC+8, 預期排程最多執行時間為 1 小時 (08:00 UTC+8 結束)
		// 2. 查詢日期: 以每日 8:00 UTC+8 為準
		// 3. 每筆 event 資料存放於 redis sorted list, 並以保存時間作為該 event 的 score
		// 4. 若查詢時間與當日日期相同, event 資料保存至隔日 08:00 UTC+8
		// 5. 若查詢時間與當日日期不同 (發生於排程執行時間中), 則資料保存時間為 5 分鐘

		if (events.Length == 0)
		{
			var expired = currentDate == queryDate
				? new DateTimeOffset(currentDate.Year, currentDate.Month, currentDate.Day + 1, 8, 0, 0,
					TimeSpan.FromHours(8D))
				: now.AddMinutes(5D);

			var info = new DepositAccumulateRouletteEventInfo(
				playerCode, promotionId, currentDate, 0, 0);

			_ = await _storeRepository
				.SetDepositAccumulateRouletteEventInfoAsync(promotionId, playerCode, expired, info)
				.ConfigureAwait(false);

			yield break;
		}

		foreach (var evt in events)
		{
			var expired = currentDate == queryDate
				? new DateTimeOffset(evt.Date.Year, evt.Date.Month, evt.Date.Day + 1, 8, 0, 0,
					TimeSpan.FromHours(8D))
				: now.AddMinutes(5D);

			_ = await _storeRepository
				.SetDepositAccumulateRouletteEventInfoAsync(promotionId, playerCode, expired, evt)
				.ConfigureAwait(false);

			yield return evt;
		}
	}
}
