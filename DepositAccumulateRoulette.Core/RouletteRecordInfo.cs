namespace Tgs.Promotion.DepositAccumulateRouletteService;

public record RouletteRecordInfo(
	Guid RouletteId,
	DateOnly EventDate,
	int ItemIndex,
	DateTimeOffset CreatedOn);
