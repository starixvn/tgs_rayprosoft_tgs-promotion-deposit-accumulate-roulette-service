namespace Tgs.Promotion.DepositAccumulateRoulette.Background.Core;

public record PromotionInfo(
	Guid Id,
	DateTime From,
	DateTime To,
	Guid BrandId,
	string CampaignDetail);
