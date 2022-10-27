namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

public record GetRouletteRecordsQuery(
	Guid PromotionId,
	string PlayerCode);
