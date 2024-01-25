using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class RouletteRecordsQueryService(
	ITgsRepository tgsRepository) : IQueryService<RouletteRecordsQuery, RouletteRecordInfo>
{
	private readonly ITgsRepository _tgsRepository = tgsRepository;

	public IAsyncEnumerable<RouletteRecordInfo> QueryAsync(
		RouletteRecordsQuery query,
		CancellationToken cancellationToken = default)
		=> _tgsRepository.QueryRouletteRecordListAsync(
			query.PromotionId,
			query.PlayerCode,
			cancellationToken);
}
