namespace Tgs.Promotion.DepositAccumulateRouletteServices;

public record RouletteResult(
	Guid? Id,
	int ItemIndex,
	int RemainingCount);
