namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal record EnqueueDistributeBonusJobCommand(
	Guid PromotionId,
	string PlayerCode,
	DateOnly EventDate,
	int UsedCount,
	PlayRouletteResult.RouletteResult PlayResult);
