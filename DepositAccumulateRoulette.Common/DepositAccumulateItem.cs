namespace Tgs.Promotion.DepositAccumulateRoulette.Common;

public record DepositAccumulateItem(
    Guid Id,
    decimal AccumulateAmount,
    int RouletteCount);