using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRouletteService;

public record DepositAccumulateRoulettePromotionInfo(
	Guid Id,
	During EventDuring,
	decimal? PromotionLimitAmount,
	bool IsAutoSent,
	DepositAccumulateRouletteSettings CampaignDetail);
