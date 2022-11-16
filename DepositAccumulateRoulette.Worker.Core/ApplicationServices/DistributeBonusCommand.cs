namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

public record DistributeBonusCommand(
	Guid PromotionId,
	string PlayerCode,
	int DistributePlanHash,
	decimal BonusAmount,
	decimal BucketAmount,
	Guid RouletteRecordId);
