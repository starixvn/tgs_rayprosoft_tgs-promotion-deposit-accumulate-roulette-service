namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal record GetAvailableRouletteCountEventQuery(
	Guid PromotionId,
	string PlayerCode);
