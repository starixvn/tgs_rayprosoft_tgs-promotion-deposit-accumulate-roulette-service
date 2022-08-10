namespace Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;

internal record DistributeBonusCommand(
	Guid PromotionId,
	string PlayerCode,
	Guid DepositAccumulateRouletteEventId,
	int CurrentRouletteCount,
	decimal Bonus,
	decimal Bucket);
