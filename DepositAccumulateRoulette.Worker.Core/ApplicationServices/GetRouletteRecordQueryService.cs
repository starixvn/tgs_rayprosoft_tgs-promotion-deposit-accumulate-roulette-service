using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class GetRouletteRecordQueryService(
	ITgsRepository tgsRepository) : IGetService<GetRouletteRecordQuery, RouletteRecordInfo?>
{
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

	public async ValueTask<RouletteRecordInfo?> GetAsync(
		GetRouletteRecordQuery query,
		CancellationToken cancellationToken = default)
	{
		var record = await _tgsRepository
			.GetRouletteRecordInfoAsync(query.RecordId, cancellationToken)
			.ConfigureAwait(false);

		return record?.BrandId == query.BrandId ? record : null;
	}
}
