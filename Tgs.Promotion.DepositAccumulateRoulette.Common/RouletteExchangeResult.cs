namespace Tgs.Promotion.DepositAccumulateRoulette.Common;

public record RouletteExchangeResult(
    RouletteResult? Result,
    int RemainingCount);