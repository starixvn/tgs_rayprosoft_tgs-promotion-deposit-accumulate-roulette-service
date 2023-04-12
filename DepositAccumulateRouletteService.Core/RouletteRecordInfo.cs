namespace Tgs.Promotion.DepositAccumulateRouletteService;

public record RouletteRecordInfo(
	Guid RouletteId,
	string PlayerCode,
	Guid PromotionId,
	DateOnly EventDate,
	int ItemIndex,
	DateTimeOffset CreatedOn);
