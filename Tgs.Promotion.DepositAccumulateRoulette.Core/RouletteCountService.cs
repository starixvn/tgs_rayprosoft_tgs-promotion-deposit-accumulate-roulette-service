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
			var before = await GetRouletteCountAsync(promotionId, playerCode, info.Date);
			if (!before.HasValue)
			{
				var usedCount = await _usedCountQueryService.GetAsync(
								new RouletteUsedCountQuery(promotionId, playerCode, info.EventId),
								cancellationToken).ConfigureAwait(false);

				before = info.RouletteCount - usedCount;
				await SetRouletteCountAsync(promotionId, playerCode, info.Date, before.Value);
			}

			var after = before.Value;
			if (before.Value > 0 && useEvent is null)
			{
				after = await DecreaseRouletteCountAsync(promotionId, playerCode, info.Date).ConfigureAwait(false);
			}

			counts[info.Date] = new CountInfo(before.Value, after);
			if (after < 0)
				continue;

			useEvent ??= info;
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

	private async ValueTask<int?> GetRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date)
	{
		var key = $"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}:{date:O}";

		var value = await _redisDatabase.StringGetAsync(key).ConfigureAwait(false);

		return value.HasValue ? (int)value! : null;
	}

	private async ValueTask SetRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date, int count)
	{
		var key = $"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}:{date:O}";

		_ = await _redisDatabase.StringSetAsync(key, count, TimeSpan.FromDays(2)).ConfigureAwait(false);
	}

	private async ValueTask<int> DecreaseRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date)
	{
		var key = $"Promotion.DepositAccumulateRoulette:RouletteCount:{promotionId}:{playerCode}:{date:O}";

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
