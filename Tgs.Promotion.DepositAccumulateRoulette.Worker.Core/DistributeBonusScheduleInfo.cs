namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal record DistributeBonusScheduleInfo(
	Guid PromotionId,
	string PlayerCode,
	DateOnly EventDate,
	int UsedCount,
	PlayRouletteResult.RouletteResult PlayResult);
