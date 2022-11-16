namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

public record RouletteRecordsQuery(
	Guid PromotionId,
	string PlayerCode);
