using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;

internal class GetRouletteUsedCountQueryService : IGetService<RouletteUsedCountQuery, int>
{
	private readonly ITgsRepository _tgsRepository;

	public GetRouletteUsedCountQueryService(
		ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public ValueTask<int> GetAsync(
		RouletteUsedCountQuery query,
		CancellationToken cancellationToken = default)
		=> _tgsRepository
			.GetRouletteUsedCountAsync(
				query.PromotionId,
				query.PlayerCode,
				query.DepositAccumulateRouletteEventId,
				cancellationToken);
}
