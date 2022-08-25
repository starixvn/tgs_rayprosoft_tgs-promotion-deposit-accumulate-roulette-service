using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class RouletteCountService : IRouletteCountService
{
	private readonly IDepositAccumulateRouletteEventRepository _eventRepository;
	private readonly IRouletteCountRepository _countRepository;
	private readonly IGetService<RouletteUsedCountQuery, int> _usedCountQueryService;

	public RouletteCountService(
		IDepositAccumulateRouletteEventRepository eventRepository,
		IRouletteCountRepository countRepository,
		IGetService<RouletteUsedCountQuery, int> usedCountQueryService)
	{
		_eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
		_countRepository = countRepository ?? throw new ArgumentNullException(nameof(countRepository));
		_usedCountQueryService = usedCountQueryService ?? throw new ArgumentNullException(nameof(usedCountQueryService));
	}

	public async ValueTask<int> GetAvailableRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default)
	{
		var events = await _eventRepository
			.GetDepositAccumulateRouletteEventInfosAsync(promotionId, playerCode, cancellationToken)
			.ToArrayAsync(cancellationToken).ConfigureAwait(false);

		var totalCount = 0;

		foreach (var info in events)
		{
			var cachedCount = await _countRepository
				.GetRouletteCountAsync(promotionId, playerCode, info.Date)
				.ConfigureAwait(false);

			var count = cachedCount ??
						await SetRouletteCountAsync(promotionId, playerCode, info, cancellationToken)
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
		var events = await _eventRepository
			.GetDepositAccumulateRouletteEventInfosAsync(promotionId, playerCode, cancellationToken)
			.ToArrayAsync(cancellationToken).ConfigureAwait(false);

		DepositAccumulateRouletteEventInfo? useEvent = null;
		var counts = new Dictionary<DateOnly, CountInfo>();

		foreach (var info in events)
		{
			var cachedCount = await _countRepository
				.GetRouletteCountAsync(promotionId, playerCode, info.Date)
				.ConfigureAwait(false);

			var before = cachedCount ??
						 await SetRouletteCountAsync(promotionId, playerCode, info, cancellationToken)
							 .ConfigureAwait(false);

			var after = before;
			if (before > 0 && useEvent is null)
			{
				after = await _countRepository
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
	{
		_ = await _countRepository
			.IncreaseRouletteCountAsync(promotionId, playerCode, countDistributeDate)
			.ConfigureAwait(false);
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

		_ = await _countRepository
			.SetRouletteCountAsync(promotionId, playerCode, eventInfo.Date, count)
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
