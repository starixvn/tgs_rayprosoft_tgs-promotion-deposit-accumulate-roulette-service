using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRouletteServices;

public record DepositAccumulateRoulettePromotion(
	Guid Id,
	decimal? PromotionLimitAmount,
	DepositAccumulateRouletteSettings CampaignDetail);
