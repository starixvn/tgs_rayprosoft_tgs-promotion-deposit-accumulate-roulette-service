namespace Tgs.DepositAccumulateRoulette.Clients;

public record RouletteRecord(
	Guid Id,
	DateOnly Date,
	int ItemIndex,
	DateTimeOffset CreatedOn);
