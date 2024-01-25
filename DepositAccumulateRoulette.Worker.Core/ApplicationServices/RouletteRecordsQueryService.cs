using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class RouletteRecordsQueryService : IQueryService<RouletteRecordsQuery, RouletteRecordInfo>
{
	private readonly ITgsRepository _tgsRepository;

	public RouletteRecordsQueryService(
		ITgsRepository tgsRepository) => _tgsRepository = tgsRepository;

	public IAsyncEnumerable<RouletteRecordInfo> QueryAsync(
		RouletteRecordsQuery query,
		CancellationToken cancellationToken = default)
		=> _tgsRepository.QueryRouletteRecordListAsync(
			query.PromotionId,
			query.PlayerCode,
			cancellationToken);
}
