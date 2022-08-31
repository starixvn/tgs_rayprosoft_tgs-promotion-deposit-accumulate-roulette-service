using System.Runtime.CompilerServices;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class StatisticsService : IStatisticsService
{
	private readonly ISystemClock _systemClock;
	private readonly ITgsRepository _tgsRepository;
	private readonly IRedisRepository _redisRepository;

	public StatisticsService(
		ISystemClock systemClock,
		ITgsRepository tgsRepository,
		IRedisRepository redisRepository)
	{
		_systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_redisRepository = redisRepository ?? throw new ArgumentNullException(nameof(redisRepository));
	}

	public ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default)
		=> _redisRepository
			.IncrementTotalDistributeAmountAsync(promotionId, amount, cancellationToken);

	public async ValueTask<int> GetAvailableRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default)
	{
		var events = await GetDepositAccumulateRouletteEventInfosAsync(
				promotionId,
				playerCode,
				cancellationToken)
			.ToArrayAsync(cancellationToken).ConfigureAwait(false);

		var totalCount = 0;

		foreach (var info in events)
		{
			var count = await GetOrSetRouletteCountAsync(promotionId, playerCode, info, cancellationToken)
				.ConfigureAwait(false);

			totalCount += count;
		}

		return totalCount;
	}

	public async ValueTask<RouletteCountInfo> DecreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default)
	{
		var events = await GetDepositAccumulateRouletteEventInfosAsync(
				promotionId,
				playerCode,
				cancellationToken)
			.OrderBy(evt => evt.Date)
			.ToArrayAsync(cancellationToken).ConfigureAwait(false);

		DepositAccumulateRouletteEventInfo? useEvent = null;
		var counts = new Dictionary<DateOnly, CountInfo>();

		foreach (var info in events)
		{
			var before = await GetOrSetRouletteCountAsync(promotionId, playerCode, info, cancellationToken)
				.ConfigureAwait(false);

			var after = before;
			if (before > 0 && useEvent is null)
			{
				after = await _redisRepository
					.DecreaseRouletteCountAsync(promotionId, playerCode, info.Date)
					.ConfigureAwait(false);

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
		=> await _redisRepository
			.IncreaseRouletteCountAsync(promotionId, playerCode, countDistributeDate)
			.ConfigureAwait(false);

	private async IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var now = _systemClock.UtcNow.ToOffset(TimeSpan.FromHours(8D));
		var currentDate = DateOnly.FromDateTime(now.DateTime);

		// assume the query date is 08:00 UTC+8
		var queryDate = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(8 - 8)).DateTime);

		var events = await _redisRepository
			.GetDepositAccumulateRouletteEventInfosAsync(promotionId, playerCode, now)
			.ToArrayAsync(cancellationToken);

		if (events.Length > 0)
		{
			foreach (var evt in events)
				yield return evt;
		}
		else
		{
			await foreach (var evt in
						   QueryAndSetDepositAccumulateRouletteEventInfosQueryAndSetEventsAsync(
							   promotionId,
							   playerCode,
							   queryDate,
							   currentDate,
							   now,
							   cancellationToken))
				yield return evt;
		}
	}

	private async IAsyncEnumerable<DepositAccumulateRouletteEventInfo> QueryAndSetDepositAccumulateRouletteEventInfosQueryAndSetEventsAsync(
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
			.ToArrayAsync(cancellationToken);

		var isCurrentDateEqualsQueryDate = currentDate == queryDate;

		if (events.Length == 0)
		{
			var expired = isCurrentDateEqualsQueryDate
				? new DateTimeOffset(currentDate.Year, currentDate.Month, currentDate.Day + 1, 8, 0, 0,
					TimeSpan.FromHours(8D))
				: now.AddMinutes(5D);

			var info = new DepositAccumulateRouletteEventInfo(
				playerCode, promotionId, currentDate, 0, 0);

			_ = await _redisRepository
				.SetDepositAccumulateRouletteEventInfoAsync(promotionId, playerCode, expired, info)
				.ConfigureAwait(false);

			yield break;
		}

		foreach (var evt in events)
		{
			var expired = isCurrentDateEqualsQueryDate
				? new DateTimeOffset(evt.Date.Year, evt.Date.Month, evt.Date.Day + 1, 8, 0, 0,
					TimeSpan.FromHours(8D))
				: now.AddMinutes(5D);

			_ = await _redisRepository
				.SetDepositAccumulateRouletteEventInfoAsync(promotionId, playerCode, expired, evt)
				.ConfigureAwait(false);

			yield return evt;
		}
	}

	private async ValueTask<int> GetOrSetRouletteCountAsync(
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
