namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public record RouletteRecordInfo(
	Guid RouletteId,
	string PlayerCode,
	Guid PromotionId,
	DateOnly EventDate,
	int ItemIndex,
	decimal BonusAmount,
	DateTime CreatedOn);
