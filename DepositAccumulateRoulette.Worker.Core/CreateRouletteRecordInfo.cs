namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public record CreateRouletteRecordInfo(
	Guid PromotionId,
	string PlayerCode,
	Guid BrandId,
	DateOnly EventDate,
	int RouletteCount,
	decimal BonusAmount,
	decimal BucketAmount,
	int ItemIndex);
