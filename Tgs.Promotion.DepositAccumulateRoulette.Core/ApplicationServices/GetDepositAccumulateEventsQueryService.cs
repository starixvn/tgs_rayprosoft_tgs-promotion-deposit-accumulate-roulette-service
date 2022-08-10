using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;

internal class GetDepositAccumulateEventsQueryService : IQueryService<GetDepositAccumulateEventsQuery, DepositAccumulateRouletteEventInfo>
{
	private readonly ITgsRepository _tgsRepository;

	public GetDepositAccumulateEventsQueryService(
		ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	}

	public IAsyncEnumerable<DepositAccumulateRouletteEventInfo> QueryAsync(GetDepositAccumulateEventsQuery query,
		CancellationToken cancellationToken = default)
		=> _tgsRepository
			.GetDepositAccumulateRouletteEventInfosAsync(
				query.PromotionId,
				query.PlayerCode,
				query.QueryDate);
}
