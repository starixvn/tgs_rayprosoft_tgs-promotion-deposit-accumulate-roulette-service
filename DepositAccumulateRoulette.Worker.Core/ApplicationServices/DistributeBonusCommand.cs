namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

public record DistributeBonusCommand(
	Guid PromotionId,
	string PlayerCode,
	DateOnly EventDate,
	int CurrentRouletteCount,
	decimal Bonus,
	decimal Bucket);
