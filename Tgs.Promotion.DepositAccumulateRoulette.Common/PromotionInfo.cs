namespace Tgs.Promotion.DepositAccumulateRoulette.Common;

public record PromotionInfo(
    Guid Id,
    DateTime From,
    DateTime To,
    Guid BrandId,
    string CampaignDetail);