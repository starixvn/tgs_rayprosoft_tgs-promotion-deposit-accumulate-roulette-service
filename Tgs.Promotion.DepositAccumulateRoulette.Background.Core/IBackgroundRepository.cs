namespace Tgs.Promotion.DepositAccumulateRoulette.Background;

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
