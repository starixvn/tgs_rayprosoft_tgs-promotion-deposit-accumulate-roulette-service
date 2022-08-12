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
	private readonly ISystemClock _systemClock;

	public RouletteCountService(
		IDatabase redisDatabase,
		IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo> depositEventQueryService,
		IGetService<RouletteUsedCountQuery, int> usedCountQueryService,
		ISystemClock systemClock)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
		_depositEventQueryService = depositEventQueryService ?? throw new ArgumentNullException(nameof(depositEventQueryService));
		_usedCountQueryService = usedCountQueryService ?? throw new ArgumentNullException(nameof(usedCountQueryService));
		_systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
	}

	public async ValueTask<RouletteCountInfo> DecreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default)
	{
		var now = _systemClock.UtcNow;

		// assume the query date is 7:00 UTC+8
		var queryDate = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(8 - 7)).DateTime);

		var events = await _depositEventQueryService
			.QueryAsync(new GetDepositAccumulateEventsQuery(promotionId, playerCode, queryDate), cancellationToken)
			.ToArrayAsync(cancellationToken).ConfigureAwait(false);

		DepositAccumulateRouletteEventInfo? useEvent = null;
		var counts = new Dictionary<DateOnly, CountInfo>();

		foreach (var info in events)
		{
			var cachedCount = await GetRouletteCountAsync(promotionId, playerCode, info.Date)
				.ConfigureAwait(false);

			var before = cachedCount ??
						 await SetRouletteCountAsync(promotionId, playerCode, info, cancellationToken)
							 .ConfigureAwait(false);

			var after = before;
			if (before > 0 && useEvent is null)
			{
				after = await DecreaseRouletteCountAsync(promotionId, playerCode, info.Date).ConfigureAwait(false);

				if (after >= 0)
					useEvent = info;
			}

			counts[info.Date] = new CountInfo(before, after);
		}

		var countInfo = useEvent == null ? null : counts[useEvent.Date];

		var totalCountInfo =
			counts.Count == 0
				? new CountInfo(0, 0)
				: counts.Values.Aggregate((a, b) =>
				{
					var before = a.Before + b.Before;
					var after = a.After + b.After;
					return new CountInfo(before, after);
				});

		return new RouletteCountInfo(
			useEvent,
			countInfo?.Before ?? 0,
			countInfo?.After ?? 0,
			totalCountInfo.Before,
			totalCountInfo.After);
	}

	public async ValueTask IncreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly countDistributeDate,
		CancellationToken cancellationToken = default)
	{
		var key = GetCountKey(promotionId, playerCode, countDistributeDate);

		_ = await _redisDatabase.StringIncrementAsync(key).ConfigureAwait(false);
	}

	private static string GetCountKey(Guid promotionId, string playerCode, DateOnly date)
	{
		var key = $"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}:{date:O}";
		return key;
	}

	private async ValueTask<int> SetRouletteCountAsync(Guid promotionId,
		string playerCode,
		DepositAccumulateRouletteEventInfo eventInfo,
		CancellationToken cancellationToken)
	{
		var usedCount = await _usedCountQueryService.GetAsync(
			new RouletteUsedCountQuery(promotionId, playerCode, eventInfo.EventId),
			cancellationToken).ConfigureAwait(false);

		var count = eventInfo.RouletteCount >= usedCount ? eventInfo.RouletteCount - usedCount : 0;

		var key = GetCountKey(promotionId, playerCode, eventInfo.Date);

		_ = await _redisDatabase.StringSetAsync(key, count, TimeSpan.FromDays(2)).ConfigureAwait(false);

		return count;
	}

	private async ValueTask<int?> GetRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date)
	{
		var key = GetCountKey(promotionId, playerCode, date);

		var value = await _redisDatabase.StringGetAsync(key).ConfigureAwait(false);

		return value.HasValue ? (int)value! : null;
	}

	private async ValueTask<int> DecreaseRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date)
	{
		var key = GetCountKey(promotionId, playerCode, date);

		return (int)await _redisDatabase.StringDecrementAsync(key).ConfigureAwait(false);
	}

	private class CountInfo
	{
		public int After { get; }

		public int Before { get; }

		public CountInfo(int before, int after)
		{
			Before = before < 0 ? 0 : before;
			After = after < 0 ? 0 : after;
		}
	}
}
