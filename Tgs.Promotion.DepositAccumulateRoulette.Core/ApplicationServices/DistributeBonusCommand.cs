namespace Tgs.Promotion.DepositAccumulateRouletteServices.ApplicationServices;

internal record DistributeBonusCommand(
	Guid PromotionId,
	string PlayerCode,
	Guid DepositAccumulateRouletteEventId,
	int CurrentRouletteCount,
	decimal Bonus,
	decimal Bucket);
