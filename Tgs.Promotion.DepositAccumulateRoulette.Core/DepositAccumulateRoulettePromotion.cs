using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

public record DepositAccumulateRoulettePromotion(
	Guid Id,
	decimal? PromotionLimitAmount,
	DepositAccumulateRouletteSettings CampaignDetail);
