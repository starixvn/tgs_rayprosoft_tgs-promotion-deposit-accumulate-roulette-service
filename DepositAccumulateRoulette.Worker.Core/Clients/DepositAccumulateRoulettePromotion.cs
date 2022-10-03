using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Clients;

internal record DepositAccumulateRoulettePromotion(
	Guid Id,
	EventDuring EventDuring,
	decimal? PromotionLimitAmount,
	DepositAccumulateRouletteSettings CampaignDetail);
