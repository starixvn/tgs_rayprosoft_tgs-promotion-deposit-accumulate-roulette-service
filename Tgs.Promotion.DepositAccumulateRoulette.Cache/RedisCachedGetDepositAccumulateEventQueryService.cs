using System.Runtime.CompilerServices;
using System.Text.Json;
using DateOnlyTimeOnly.AspNet.Converters;
using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Cache;

internal class RedisCachedGetDepositAccumulateEventQueryService :
	IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo>
{
	private readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		Converters = { new DateOnlyJsonConverter() }
	};

	private readonly IDatabase _redisDatabase;
	private readonly IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo> _queryService;
	private readonly ISystemClock _systemClock;

	public RedisCachedGetDepositAccumulateEventQueryService(
		IDatabase redisDatabase,
		IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo> queryService,
		ISystemClock systemClock)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
		_queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
		_systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
	}

	public async IAsyncEnumerable<DepositAccumulateRouletteEventInfo> QueryAsync(GetDepositAccumulateEventsQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var key = $"Promotion:DepositAccumulateRoulette:DepositEventInfo:{query.PromotionId}:{query.PlayerCode}";

		var now = _systemClock.UtcNow.ToOffset(TimeSpan.FromHours(8D));
		var currentDate = DateOnly.FromDateTime(now.DateTime);
		var queryDateTime = query.QueryDate.ToDateTime(new TimeOnly(7, 0));
		var score = queryDateTime.Ticks;

		var values = await _redisDatabase
			.SortedSetRangeByScoreAsync(key, start: score, exclude: Exclude.None, order: Order.Ascending)
			.ConfigureAwait(false);

		if (values.Length > 0)
		{
			foreach (var value in values)
			{
				yield return JsonSerializer.Deserialize<DepositAccumulateRouletteEventInfo>(
					value!,
					_jsonSerializerOptions)!;
			}
		}
		else
		{
			var events = await _queryService
				.QueryAsync(query, cancellationToken)
				.ToArrayAsync(cancellationToken)
				.ConfigureAwait(false);

			foreach (var evt in events)
			{
				var data = JsonSerializer.Serialize(evt, _jsonSerializerOptions);
				var expired = currentDate == query.QueryDate
					? evt.Date.AddDays(1).ToDateTime(new TimeOnly(8, 0)).Ticks
					: now.AddMinutes(5D).Ticks;

				_ = await _redisDatabase.SortedSetAddAsync(key, data, expired)
					.ConfigureAwait(false);

				yield return evt;
			}
		}
	}
}
