using Tgs.Promotion.DepositAccumulateRoulette.Worker.Clients;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal interface IDepositAccumulateRouletteServiceClient
{
	public ValueTask<DepositAccumulateRoulettePromotion?> GetDepositAccumulateRoulettePromotionAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default);
}
