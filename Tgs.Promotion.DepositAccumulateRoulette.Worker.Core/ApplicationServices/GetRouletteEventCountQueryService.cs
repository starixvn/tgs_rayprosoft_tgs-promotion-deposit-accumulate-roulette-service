using System.Runtime.CompilerServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class GetRouletteEventCountQueryService : IQueryService<GetRouletteEventCountQuery, RouletteEventCountInfo>
{
	private readonly IDepositAccumulateRouletteEventService _eventService;
	private readonly IRedisRepository _redisRepository;
	private readonly ITgsRepository _tgsRepository;

	public GetRouletteEventCountQueryService(
		IDepositAccumulateRouletteEventService eventService,
		IRedisRepository redisRepository,
		ITgsRepository tgsRepository)
	{
		_eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
		_redisRepository = redisRepository ?? throw new ArgumentNullException(nameof(redisRepository));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public async IAsyncEnumerable<RouletteEventCountInfo> QueryAsync(GetRouletteEventCountQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var events = await _eventService
			.GetDepositAccumulateRouletteEventInfosAsync(
				query.PromotionId,
				query.PlayerCode,
				cancellationToken)
			.ToArrayAsync(cancellationToken);

		foreach (var info in events)
		{
			var count = await GetOrSetRouletteCountAsync(query.PromotionId, query.PlayerCode, info, cancellationToken)
				.ConfigureAwait(false);

			yield return new RouletteEventCountInfo(info, count);
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
}
