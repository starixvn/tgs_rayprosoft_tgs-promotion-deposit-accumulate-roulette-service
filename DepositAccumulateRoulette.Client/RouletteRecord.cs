namespace Tgs.DepositAccumulateRoulette.Clients;

public record RouletteRecord(
	Guid RouletteId,
	string PlayerCode,
	Guid PromotionId,
	DateOnly EventDate,
	int ItemIndex,
	DateTimeOffset CreatedOn);
