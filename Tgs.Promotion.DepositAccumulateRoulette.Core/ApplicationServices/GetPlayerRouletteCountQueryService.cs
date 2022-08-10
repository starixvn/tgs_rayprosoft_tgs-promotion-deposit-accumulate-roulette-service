using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;

internal class GetPlayerRouletteCountQueryService : IGetService<GetPlayerRouletteCountQuery, RouletteCountInfo?>
{
    public async ValueTask<RouletteCountInfo?> GetAsync(
        GetPlayerRouletteCountQuery query,
        CancellationToken cancellationToken = default)
    {
        return new RouletteCountInfo(query.PromotionId, 3);
    }
}