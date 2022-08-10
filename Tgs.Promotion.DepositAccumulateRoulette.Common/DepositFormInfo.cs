namespace Tgs.Promotion.DepositAccumulateRoulette.Common;

public record DepositFormInfo(
    Guid FormId,
    string PlayerCode,
    Guid BrandId,
    Guid PromotionId,
    decimal? ActualAmount,
    decimal Amount);