namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal record GetRouletteEventCountQuery(
	Guid PromotionId,
	string PlayerCode);
