using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

internal class RouletteCountService : IRouletteCountService
{
	private readonly IDatabase _redisDatabase;
	private readonly IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo> _depositEventQueryService;
	private readonly IGetService<RouletteUsedCountQuery, int> _usedCountQueryService;

	public RouletteCountService(
		IDatabase redisDatabase,
		IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo> depositEventQueryService,
		IGetService<RouletteUsedCountQuery, int> usedCountQueryService)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
		_depositEventQueryService = depositEventQueryService ?? throw new ArgumentNullException(nameof(depositEventQueryService));
		_usedCountQueryService = usedCountQueryService ?? throw new ArgumentNullException(nameof(usedCountQueryService));
	}

	public ValueTask<RouletteCountInfo> DecreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default)
	{
		//var countKey = GetRouletteCountRedisKey(promotionId, playerCode, depositAccumulateRouletteEventId);

		//var keyExist = await _redisDatabase.KeyExistsAsync(countKey).ConfigureAwait(false)''
		//if ()

		//	var count = await _redisDatabase.StringDecrementAsync(countKey).ConfigureAwait(false);
		//return (int)count;

		var info = new RouletteCountInfo(null, 0, 0, 0, 0);
		return ValueTask.FromResult(info);
	}

	public ValueTask<RouletteCountInfo> IncreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly countDistributeDate,
		CancellationToken cancellationToken = default)
	{
		//var countKey = GetRouletteCountRedisKey(promotionId, playerCode, depositAccumulateRouletteEventId);
		//var count = await _redisDatabase.StringIncrementAsync(countKey).ConfigureAwait(false);
		//return (int)count;

		var info = new RouletteCountInfo(null, 0, 0, 0, 0);
		return ValueTask.FromResult(info);
	}

	private static string GetEventIdRedisKey(Guid promotionId, string playerCode, DateOnly queryDate)
		=> $"Promotion.DepositAccumulateRoulette:EventId:{promotionId}:{playerCode}:{queryDate}";

	private static string GetRouletteCountRedisKey(Guid promotionId, string playerCode, Guid? eventId)
		=> eventId.HasValue
			? $"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}:{eventId:D}"
			: $"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}:null";

	//private async ValueTask<RouletteCountInfo> GetRemainingRouletteCountAsync(
	//	Guid promotionId,
	//	string playerCode,
	//	CancellationToken cancellationToken = default)
	//{
	//	var now = DateTimeOffset.Now;

	//	// assume the change date is 8:00 UTC+8
	//	var recordDate = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(8 - 8)).AddMinutes(-1).DateTime);

	//	var eventIdKey = GetEventIdRedisKey(promotionId, playerCode, recordDate);
	//	var eventIdValue = await _redisDatabase.StringGetAsync(eventIdKey).ConfigureAwait(false);
	//	var eventId = Guid.TryParse(eventIdValue, out var id) && id != Guid.Empty ? id : (Guid?)null;

	//	var countKey = GetRouletteCountRedisKey(promotionId, playerCode, eventId);
	//	var countValue = await _redisDatabase.StringGetAsync(countKey).ConfigureAwait(false);

	//	return countValue.HasValue
	//		? new RouletteCountInfo(eventId, (int)countValue)
	//		: await GetRemainingRouletteCountFromDatabaseAsync(promotionId, playerCode, recordDate, cancellationToken)
	//			.ConfigureAwait(false);
	//}

	//private async Task<RouletteCountInfo> GetRemainingRouletteCountFromDatabaseAsync(
	//	Guid promotionId,
	//	string playerCode,
	//	DateOnly recordDate,
	//	CancellationToken cancellationToken)
	//{
	//	var events = await _depositEventQueryService
	//		.QueryAsync(new GetDepositAccumulateEventsQuery(promotionId, playerCode, recordDate), cancellationToken)
	//		.ToArrayAsync(cancellationToken)
	//		.ConfigureAwait(false);

	//	if (events.Length == 0)
	//	{
	//		var eventIdKey = GetEventIdRedisKey(promotionId, playerCode, recordDate);
	//		_ = await _redisDatabase.StringSetAsync(eventIdKey, Guid.Empty.ToString("D"), TimeSpan.FromDays(1), When.NotExists).ConfigureAwait(false);

	//		var countKey = GetRouletteCountRedisKey(promotionId, playerCode, null);
	//		_ = await _redisDatabase.StringSetAsync(countKey, 0, TimeSpan.FromDays(1), When.NotExists).ConfigureAwait(false);

	//		return new RouletteCountInfo(null, 0);
	//	}

	//	foreach (var evt in events.OrderBy(e => e.Date))
	//	{
	//		var index = Array.IndexOf(events, evt);

	//		var countKey = GetRouletteCountRedisKey(promotionId, playerCode, evt.EventId);
	//		var countValue = await _redisDatabase.StringGetAsync(countKey).ConfigureAwait(false);

	//		var count = countValue.HasValue ? (int)countValue : 0;
	//		if (count > 0)
	//		{
	//			return new RouletteCountInfo(evt.EventId, count);
	//		}

	//		var usedCount = await _usedCountQueryService.GetAsync(
	//			new RouletteUsedCountQuery(promotionId, playerCode, evt.EventId),
	//			cancellationToken).ConfigureAwait(false);

	//		count = evt.RouletteCount - usedCount;
	//		count = count > 0 ? count : 0;

	//		if (count > 0 || index == events.Length - 1)
	//		{
	//			var eventIdKey = GetEventIdRedisKey(promotionId, playerCode, recordDate);

	//			_ = await _redisDatabase.StringSetAsync(
	//					eventIdKey,
	//					evt.EventId.ToString("D"),
	//					TimeSpan.FromDays(1),
	//					When.Always)
	//				.ConfigureAwait(false);

	//			_ = await _redisDatabase.StringSetAsync(countKey, count, TimeSpan.FromDays(1), When.Always)
	//				.ConfigureAwait(false);

	//			return new RouletteCountInfo(evt.EventId, count);
	//		}
	//	}

	//	return new RouletteCountInfo(null, 0);
	//}
}
