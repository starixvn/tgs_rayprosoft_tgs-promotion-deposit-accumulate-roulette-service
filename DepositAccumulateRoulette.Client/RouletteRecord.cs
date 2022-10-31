namespace Tgs.DepositAccumulateRoulette.Clients;

public record RouletteRecord(
	Guid RouletteId,
	DateOnly EventDate,
	int ItemIndex,
	DateTimeOffset CreatedOn);
