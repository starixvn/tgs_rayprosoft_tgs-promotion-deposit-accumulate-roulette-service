using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.Core;

public interface IBackgroundRepository
{
    IAsyncEnumerable<PromotionInfo> GetValidDepositAccumulateRoulettePromotionsAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<DepositFormInfo> GetApprovedDepositFormsAsync(
        IEnumerable<Guid> brandIds,
        DateOnly processDate,
        CancellationToken cancellationToken = default);

    ValueTask AddDepositAccumulateRouletteEventsAsync(
        DateOnly processDate,
        IEnumerable<DepositAccumulateRouletteEventAddInfo> events,
        CancellationToken cancellationToken = default);
}