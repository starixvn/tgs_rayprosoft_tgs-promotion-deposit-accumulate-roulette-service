using DateOnlyTimeOnly.AspNet.Converters;
using System.Text.Json;
using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Worker;

namespace Tgs.Promotion.DepositAccumulateRoulette.Repositories;

internal class RedisRepository : IRedisRepository
{
	private static readonly string _KeyPrefix = "Promotion:DepositAccumulateRoulette";
	private static readonly decimal _ExchangeRate = 1_000_000M;

	private static readonly JsonSerializerOptions _DepositEventJsonSerializerOptions = new()
	{
		Converters = { new DateOnlyJsonConverter() }
	};

	private readonly IDatabase _redisDatabase;

	public RedisRepository(
		IDatabase redisDatabase)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
	}

	public async ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default)
	{
		var redisKey = $"{_KeyPrefix}:TotalUsed:{promotionId}";

		var returnAmount = await _redisDatabase
			.StringIncrementAsync(redisKey, (long)Math.Floor(amount * _ExchangeRate))
			.ConfigureAwait(false);

		return returnAmount / _ExchangeRate;
	}

	public async ValueTask<int?> GetRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date)
	{
		var redisKey = GetRouletteCountRedisKey(promotionId, playerCode, date);

		var value = await _redisDatabase.StringGetAsync(redisKey).ConfigureAwait(false);

		return value.HasValue ? (int)value! : null;
	}

	public async ValueTask<int> DecreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date)
	{
		var redisKey = GetRouletteCountRedisKey(promotionId, playerCode, date);

		return (int)await _redisDatabase.StringDecrementAsync(redisKey).ConfigureAwait(false);
	}

	public async ValueTask<int> IncreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date)
	{
		var redisKey = GetRouletteCountRedisKey(promotionId, playerCode, date);

		return (int)await _redisDatabase.StringIncrementAsync(redisKey).ConfigureAwait(false);
	}

	public async ValueTask<bool> SetRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date,
		int rouletteCount)
	{
		var redisKey = GetRouletteCountRedisKey(promotionId, playerCode, date);

		return await _redisDatabase
			.StringSetAsync(redisKey, rouletteCount, TimeSpan.FromDays(2))
			.ConfigureAwait(false);
	}

	public async IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		DateTimeOffset queryDate)
	{
		var redisKey = GetDepositEventsRedisKey(promotionId, playerCode);

		var score = queryDate.ToOffset(TimeSpan.FromHours(8)).Ticks;

		var values = await _redisDatabase
			.SortedSetRangeByScoreAsync(redisKey, start: score, exclude: Exclude.None, order: Order.Ascending)
			.ConfigureAwait(false);

		foreach (var value in values)
		{
			var eventInfo = value.HasValue
				? JsonSerializer.Deserialize<DepositAccumulateRouletteEventInfo>(value!, _DepositEventJsonSerializerOptions)
				: null;

			if (eventInfo is not null)
				yield return eventInfo;
		}
	}

	public async ValueTask<bool> SetDepositAccumulateRouletteEventInfoAsync(
		Guid promotionId,
		string playerCode,
		DateTimeOffset expireDate,
		DepositAccumulateRouletteEventInfo eventInfo)
	{
		var data = JsonSerializer.Serialize(eventInfo, _DepositEventJsonSerializerOptions);

		var redisKey = GetDepositEventsRedisKey(promotionId, playerCode);

		var score = expireDate.ToOffset(TimeSpan.FromHours(8)).Ticks;

		return await _redisDatabase.SortedSetAddAsync(redisKey, data, score)
			.ConfigureAwait(false);
	}

	public async ValueTask ClearExpiredDepositAccumulateRouletteEventsAsync(Guid promotionId, string playerCode)
	{
		var redisKey = GetDepositEventsRedisKey(promotionId, playerCode);

		var score = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8D)).Ticks;

		_ = await _redisDatabase.SortedSetRemoveRangeByScoreAsync(redisKey, start: 0, stop: score, exclude: Exclude.Stop)
			.ConfigureAwait(false);
	}

	private static string GetRouletteCountRedisKey(Guid promotionId, string playerCode, DateOnly date)
		=> $"{_KeyPrefix}:RouletteCount:{promotionId}:{playerCode}:{date:O}";

	private static string GetDepositEventsRedisKey(Guid promotionId, string playerCode)
		=> $"{_KeyPrefix}:DepositEvents:{promotionId}:{playerCode}";
}
