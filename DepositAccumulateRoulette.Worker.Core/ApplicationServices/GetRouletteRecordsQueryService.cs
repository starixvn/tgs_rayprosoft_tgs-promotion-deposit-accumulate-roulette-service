using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class GetRouletteRecordsQueryService : IQueryService<GetRouletteRecordsQuery, RouletteRecordInfo>
{
	private readonly ITgsRepository _tgsRepository;

	public GetRouletteRecordsQueryService(
		ITgsRepository tgsRepository)
	{
		_tgsRepository = tgsRepository;
	}

	public IAsyncEnumerable<RouletteRecordInfo> QueryAsync(
		GetRouletteRecordsQuery query,
		CancellationToken cancellationToken = default)
		=> _tgsRepository.GetRouletteRecordInfosAsync(query.PromotionId, query.PlayerCode);
}
