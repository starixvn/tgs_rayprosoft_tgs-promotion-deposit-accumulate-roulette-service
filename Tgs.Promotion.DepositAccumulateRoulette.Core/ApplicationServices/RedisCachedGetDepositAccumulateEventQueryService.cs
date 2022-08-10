using System.Runtime.CompilerServices;
using System.Text.Json;
using DateOnlyTimeOnly.AspNet.Converters;
using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;

internal class RedisCachedGetDepositAccumulateEventQueryService :
	IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo>,
	IClearDepositAccumulateRouletteEventCache
{
	private readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		Converters = { new DateOnlyJsonConverter() }
	};

	private readonly IDatabase _redisDatabase;
	private readonly IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo> _queryService;

	public RedisCachedGetDepositAccumulateEventQueryService(
		IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo> queryService,
		IDatabase redisDatabase)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
		_queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
	}

	public async IAsyncEnumerable<DepositAccumulateRouletteEventInfo> QueryAsync(GetDepositAccumulateEventsQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var key = $"Promotion:DepositAccumulateRoulette:DepositEventInfo:{query.PromotionId}:{query.PlayerCode}";

		var score = new DateTimeOffset(query.QueryDate.Year, query.QueryDate.Month, query.QueryDate.Day, 8, 0, 0,
			TimeSpan.FromHours(8D)).Ticks;

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
				var expired = new DateTimeOffset(evt.Date.Year, evt.Date.Month, evt.Date.Day + 1, 8, 0, 0, TimeSpan.FromHours(8D)).Ticks;

				if (score >= expired)
					continue;

				_ = await _redisDatabase.SortedSetAddAsync(key, data, expired, SortedSetWhen.NotExists).ConfigureAwait(false);

				yield return evt;
			}
		}
	}

	public async ValueTask ClearCacheAsync(Guid promotionId, string playerCode)
	{
		var key = $"Promotion:DepositAccumulateRoulette:DepositEventInfo:{promotionId}:{playerCode}";

		var score = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8D)).Ticks;

		_ = await _redisDatabase.SortedSetRemoveRangeByScoreAsync(key, start: 0, stop: score, exclude: Exclude.Stop)
			.ConfigureAwait(false);
	}
}
