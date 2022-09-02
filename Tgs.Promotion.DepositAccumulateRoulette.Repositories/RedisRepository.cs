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
		var redisKey = $"{_KeyPrefix}:RouletteCount:{promotionId}:{playerCode}:{date:O}";

		var value = await _redisDatabase.StringGetAsync(redisKey).ConfigureAwait(false);

		return value.HasValue ? (int)value! : null;
	}

	public async ValueTask<int> DecreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date)
	{
		var redisKey = $"{_KeyPrefix}:RouletteCount:{promotionId}:{playerCode}:{date:O}";

		return (int)await _redisDatabase.StringDecrementAsync(redisKey).ConfigureAwait(false);
	}

	public async ValueTask<int> IncreaseRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date)
	{
		var redisKey = $"{_KeyPrefix}:RouletteCount:{promotionId}:{playerCode}:{date:O}";

		return (int)await _redisDatabase.StringIncrementAsync(redisKey).ConfigureAwait(false);
	}

	public async ValueTask<bool> SetRouletteCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly date,
		int rouletteCount)
	{
		var redisKey = $"{_KeyPrefix}:RouletteCount:{promotionId}:{playerCode}:{date:O}";

		return await _redisDatabase
			.StringSetAsync(redisKey, rouletteCount, TimeSpan.FromDays(2))
			.ConfigureAwait(false);
	}

	public async IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		DateTimeOffset queryDate)
	{
		var redisKey = $"{_KeyPrefix}:DepositEvents:{promotionId}:{playerCode}";

		var score = queryDate.ToUnixTimeMilliseconds();

		var values = await _redisDatabase
			.SortedSetRangeByScoreWithScoresAsync(redisKey, start: score)
			.ConfigureAwait(false);

		foreach (var value in values.OrderBy(v => v.Score))
		{
			var eventInfo = value.Element.HasValue
				? JsonSerializer.Deserialize<DepositAccumulateRouletteEventInfo>(value.Element!, _DepositEventJsonSerializerOptions)
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

		var redisKey = $"{_KeyPrefix}:DepositEvents:{promotionId}:{playerCode}";

		var score = expireDate.ToUnixTimeMilliseconds();

		var isSet = await _redisDatabase.SortedSetAddAsync(redisKey, data, score)
			.ConfigureAwait(false);

		_ = await _redisDatabase.KeyExpireAsync(redisKey, DateTime.Now.AddDays(1)).ConfigureAwait(false);

		return isSet;
	}

	public async ValueTask ClearExpiredDepositAccumulateRouletteEventsAsync(Guid promotionId, string playerCode)
	{
		var redisKey = $"{_KeyPrefix}:DepositEvents:{promotionId}:{playerCode}";

		var score = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8D)).Ticks;

		_ = await _redisDatabase.SortedSetRemoveRangeByScoreAsync(redisKey, start: 0, stop: score, exclude: Exclude.Stop)
			.ConfigureAwait(false);
	}
}
