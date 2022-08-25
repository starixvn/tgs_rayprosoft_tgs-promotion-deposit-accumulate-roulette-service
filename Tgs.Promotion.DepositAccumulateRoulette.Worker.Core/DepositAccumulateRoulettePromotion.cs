using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public record DepositAccumulateRoulettePromotion(
	Guid Id,
	decimal? PromotionLimitAmount,
	DepositAccumulateRouletteSettings CampaignDetail);
