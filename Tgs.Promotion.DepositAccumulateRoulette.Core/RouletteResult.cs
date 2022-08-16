namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

public record RouletteResult(
	Guid? Id,
	int ItemIndex,
	int RemainingCount);
