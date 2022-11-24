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

	public async IAsyncEnumerable<DepositAccumulateRouletteEventInfo> QueryDepositAccumulateRouletteEventInfosAsync(
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
				queryDate,
				cancellationToken)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		// expire time 計算:
		// 1. 排程起始時間: 每日 07:00 UTC+8, 預期排程最多執行時間為 1 小時 (08:00 UTC+8 結束)
		// 2. 查詢日期: 以每日 8:00 UTC+8 為準
		// 3. 每筆 event 資料存放於 redis sorted list, 並以保存時間 (unix milliseconds) 作為該 event 的 score
		// 4. 若查詢時間與當日日期相同, event 資料保存至 event 日期的隔日 08:00 UTC+8
		// 5. 若查詢時間與當日日期不同 (發生於排程執行時間中), 則資料保存時間為 5 分鐘
		// 6. 若查詢不到 event 資料, 則補一個沒次數的 event 儲存

		var eventInfos = events.Length == 0
			? new[]
			{
				new DepositAccumulateRouletteEventInfo(
					playerCode, promotionId, currentDate, 0, 0)
			}
			: events;

		foreach (var evt in eventInfos)
		{
			var expired = now.AddMinutes(5D);

			if (currentDate == queryDate)
			{
				var nextDate = evt.Date.AddDays(1);
				expired = new DateTimeOffset(nextDate.Year, nextDate.Month, nextDate.Day, 8, 0, 0,
					TimeSpan.FromHours(8D));
			}

			_ = await _storeRepository
				.SetDepositAccumulateRouletteEventInfoAsync(promotionId, playerCode, expired, evt)
				.ConfigureAwait(false);

			yield return evt;
		}
	}
}
