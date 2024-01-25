using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class RouletteRecordsConditionQueryService :
	IQueryService<RouletteRecordsConditionQuery, RouletteRecordInfo>
{
	private readonly ITgsRepository _tgsRepository;

	public RouletteRecordsConditionQueryService(
		ITgsRepository tgsRepository) => _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

	public IAsyncEnumerable<RouletteRecordInfo> QueryAsync(RouletteRecordsConditionQuery query,
		CancellationToken cancellationToken = default)
		=> _tgsRepository
			.QueryRouletteRecordListAsync(
				query.BrandId,
				query.PlayerCodes,
				query.PromotionIds,
				query.BeginTime.UtcDateTime,
				query.EndTime.UtcDateTime,
				cancellationToken);
}
