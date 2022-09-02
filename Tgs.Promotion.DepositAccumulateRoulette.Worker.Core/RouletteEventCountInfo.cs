namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal record RouletteEventCountInfo(
	string PlayerCode,
	Guid PromotionId,
	DateOnly Date,
	int DistributeCount,
	int AvailableCount);
