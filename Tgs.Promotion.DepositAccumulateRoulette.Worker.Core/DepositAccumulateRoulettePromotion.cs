using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public record DepositAccumulateRoulettePromotion(
	Guid Id,
	During EventDuring,
	decimal? PromotionLimitAmount,
	DepositAccumulateRouletteSettings CampaignDetail);
