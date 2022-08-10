namespace Tgs.Promotion.DepositAccumulateRoulette.Background.Core;

public record DepositAccumulateRouletteEventAddInfo(
    string PlayerCode,
    Guid BrandId,
    Guid PromotionId,
    int RouletteCount,
    decimal TotalDepositAmount);