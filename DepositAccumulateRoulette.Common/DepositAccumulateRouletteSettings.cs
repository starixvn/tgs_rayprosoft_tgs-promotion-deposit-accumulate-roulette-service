namespace Tgs.Promotion.DepositAccumulateRoulette.Common;

public record DepositAccumulateRouletteSettings(
    IEnumerable<DepositAccumulateItem> DepositItems,
    IEnumerable<RouletteItem> RouletteItems);