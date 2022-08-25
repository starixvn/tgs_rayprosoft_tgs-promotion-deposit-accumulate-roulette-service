using System.Runtime.CompilerServices;
using System.Text.Json;
using DateOnlyTimeOnly.AspNet.Converters;
using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Worker;

namespace Tgs.Promotion.DepositAccumulateRoulette.Repositories;

internal class DepositAccumulateRouletteEventRepository :
	IDepositAccumulateRouletteEventRepository,
	IDepositAccumulateRouletteEventHandler
{
	private static readonly string _RedisKeyPrefix = "Promotion:DepositAccumulateRoulette:DepositEvents";

	private static readonly JsonSerializerOptions _JsonSerializerOptions = new()
	{
		Converters = { new DateOnlyJsonConverter() }
	};

	private readonly IDatabase _redisDatabase;
	private readonly ITgsRepository _tgsRepository;
	private readonly ISystemClock _systemClock;

	public DepositAccumulateRouletteEventRepository(
		IDatabase redisDatabase,
		ITgsRepository tgsRepository,
		ISystemClock systemClock)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
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

		var queryScore = now.Ticks;

		var redisKey = $"{_RedisKeyPrefix}:{promotionId}:{playerCode}";

		var values = await _redisDatabase
			.SortedSetRangeByScoreAsync(redisKey, start: queryScore, exclude: Exclude.None, order: Order.Ascending)
			.ConfigureAwait(false);

		if (values.Length > 0)
		{
			foreach (var value in values)
			{
				var eventInfo = value.HasValue
					? JsonSerializer.Deserialize<DepositAccumulateRouletteEventInfo>(value!, _JsonSerializerOptions)
					: null;

				if (eventInfo is not null && eventInfo.EventId != Guid.Empty)
					yield return eventInfo;
			}
		}
		else
		{
			var events = await _tgsRepository
				.GetDepositAccumulateRouletteEventInfosAsync(promotionId, playerCode, queryDate)
				.ToArrayAsync(cancellationToken)
				.ConfigureAwait(false);

			if (events.Length == 0)
			{
				var expired = currentDate == queryDate
					? new DateTimeOffset(currentDate.Year, currentDate.Month, currentDate.Day + 1, 8, 0, 0,
						TimeSpan.FromHours(8D)).Ticks
					: now.AddMinutes(5D).Ticks;

				var info = new DepositAccumulateRouletteEventInfo(
					Guid.Empty, playerCode, promotionId, currentDate, 0, 0);

				var data = JsonSerializer.Serialize(info, _JsonSerializerOptions);

				_ = await _redisDatabase.SortedSetAddAsync(redisKey, data, expired)
					.ConfigureAwait(false);

				yield break;
			}

			foreach (var evt in events)
			{
				var data = JsonSerializer.Serialize(evt, _JsonSerializerOptions);
				var expired = currentDate == queryDate
					? new DateTimeOffset(evt.Date.Year, evt.Date.Month, evt.Date.Day + 1, 8, 0, 0,
						TimeSpan.FromHours(8D)).Ticks
					: now.AddMinutes(5D).Ticks;

				_ = await _redisDatabase.SortedSetAddAsync(redisKey, data, expired)
					.ConfigureAwait(false);

				yield return evt;
			}
		}
	}

	public async ValueTask ClearExpiredEventsAsync(Guid promotionId, string playerCode)
	{
		var redisKey = $"{_RedisKeyPrefix}:{promotionId}:{playerCode}";

		var score = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8D)).Ticks;

		_ = await _redisDatabase.SortedSetRemoveRangeByScoreAsync(redisKey, start: 0, stop: score, exclude: Exclude.Stop)
			.ConfigureAwait(false);
	}
}
