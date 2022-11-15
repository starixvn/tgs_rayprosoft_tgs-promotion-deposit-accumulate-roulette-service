namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

public record DistributeBonusCommand(
	Guid PromotionId,
	string PlayerCode,
	int Hash,
	decimal Bonus,
	decimal Bucket,
	Guid RouletteRecordId);
