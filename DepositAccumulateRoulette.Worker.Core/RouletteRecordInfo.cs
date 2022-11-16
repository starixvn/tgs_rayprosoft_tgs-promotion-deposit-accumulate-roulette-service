namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public record RouletteRecordInfo(
	Guid RecordId,
	string PlayerCode,
	Guid PromotionId,
	DateOnly EventDate,
	int ItemIndex,
	decimal BonusAmount,
	decimal BucketAmount,
	int RouletteCount,
	DateTimeOffset CreatedOn);
