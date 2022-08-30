namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public record DepositAccumulateRouletteEventInfo(
	string PlayerCode,
	Guid PromotionId,
	DateOnly Date,
	int RouletteCount,
	decimal TotalDepositAmount);
