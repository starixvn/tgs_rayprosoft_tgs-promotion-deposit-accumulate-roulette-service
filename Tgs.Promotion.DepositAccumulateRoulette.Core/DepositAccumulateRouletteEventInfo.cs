namespace Tgs.Promotion.DepositAccumulateRouletteServices;

public record DepositAccumulateRouletteEventInfo(
	Guid EventId,
	string PlayerCode,
	Guid PromotionId,
	DateOnly Date,
	int RouletteCount,
	decimal TotalDepositAmount);
