namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public record RouletteRecordInfo(
	Guid RecordId,
	string PlayerCode,
	Guid PromotionId,
	Guid BrandId,
	DateOnly EventDate,
	int ItemIndex,
	decimal BonusAmount,
	decimal BucketAmount,
	int RouletteCount,
	DateTimeOffset CreatedOn);
