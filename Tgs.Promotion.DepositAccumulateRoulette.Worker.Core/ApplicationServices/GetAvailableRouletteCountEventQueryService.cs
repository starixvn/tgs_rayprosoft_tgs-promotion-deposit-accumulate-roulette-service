using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class GetAvailableRouletteCountEventQueryService : IGetService<GetAvailableRouletteCountEventQuery, RouletteEventCountInfo?>
{
	private readonly IDepositAccumulateRouletteEventService _eventService;
	private readonly IStoreRepository _storeRepository;

	public GetAvailableRouletteCountEventQueryService(
		IDepositAccumulateRouletteEventService eventService,
		IStoreRepository storeRepository)
	{
		_eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
		_storeRepository = storeRepository ?? throw new ArgumentNullException(nameof(storeRepository));
	}

	public async ValueTask<RouletteEventCountInfo?> GetAsync(
		GetAvailableRouletteCountEventQuery query,
		CancellationToken cancellationToken = default)
	{
		var eventInfos = await _eventService
			.GetDepositAccumulateRouletteEventInfosAsync(
				query.PromotionId,
				query.PlayerCode,
				cancellationToken)
			.OrderBy(evt => evt.Date)
			.ToArrayAsync(cancellationToken);

		foreach (var info in eventInfos)
		{
			var count = await _storeRepository
				.GetRouletteUsedCountAsync(info.PromotionId, info.PlayerCode, info.Date)
				.ConfigureAwait(false);

			if (info.RouletteCount >= count)
				return new RouletteEventCountInfo(
						info.PlayerCode,
						info.PromotionId,
						info.Date,
						info.RouletteCount,
						count.Value);
		}

		return null;
	}
}
