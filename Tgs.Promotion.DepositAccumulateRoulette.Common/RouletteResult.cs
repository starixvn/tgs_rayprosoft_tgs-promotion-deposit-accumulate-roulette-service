namespace Tgs.Promotion.DepositAccumulateRoulette.Common;

public record RouletteResult(
	Guid? Id,
	int ItemIndex,
	int RemainingCount);
