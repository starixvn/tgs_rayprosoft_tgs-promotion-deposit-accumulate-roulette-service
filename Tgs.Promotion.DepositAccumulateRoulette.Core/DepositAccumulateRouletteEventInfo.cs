namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

public record DepositAccumulateRouletteEventInfo(
	Guid EventId,
	string PlayerCode,
	Guid BrandId,
	Guid PromotionId,
	DateOnly Date,
	int RouletteCount,
	decimal TotalDepositAmount);
