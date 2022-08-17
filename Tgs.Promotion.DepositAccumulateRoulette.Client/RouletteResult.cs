namespace Tgs.DepositAccumulateRoulette.Clients;

public record RouletteResult(
	Guid? Id,
	int ItemIndex,
	int RemainingCount);
