using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class GetAvailableRouletteCountEventQueryService : IGetService<GetAvailableRouletteCountEventQuery, RouletteEventCountInfo?>
{
	private readonly IDepositAccumulateRouletteEventService _eventService;
	private readonly ITgsRepository _tgsRepository;
	private readonly IStoreRepository _storeRepository;

	public GetAvailableRouletteCountEventQueryService(
		IDepositAccumulateRouletteEventService eventService,
		ITgsRepository tgsRepository,
		IStoreRepository storeRepository)
	{
		_eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
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
			.ToArrayAsync(cancellationToken);

		foreach (var info in eventInfos)
		{
			var count = await _storeRepository
				.GetRouletteCountAsync(info.PromotionId, info.PlayerCode, info.Date)
				.ConfigureAwait(false);

			switch (count)
			{
				// assume share storage should keep the count already
				case null:
					return null;

				case > 0:
					return new RouletteEventCountInfo(
						info.PlayerCode,
						info.PromotionId,
						info.Date,
						info.RouletteCount,
						count.Value);
			}
		}

		return null;
	}
}
