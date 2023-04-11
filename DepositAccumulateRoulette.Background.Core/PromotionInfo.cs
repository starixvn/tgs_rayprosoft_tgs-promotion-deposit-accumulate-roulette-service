using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background;

public record PromotionInfo(
	Guid Id,
	DateTime From,
	DateTime To,
	Guid BrandId,
	DepositAccumulateRouletteSettings CampaignDetail);
