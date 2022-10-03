namespace Tgs.Promotion.DepositAccumulateRoulette.Background;

public record DepositAccumulateRouletteEventAddInfo(
	string PlayerCode,
	Guid BrandId,
	Guid PromotionId,
	int RouletteCount,
	decimal TotalDepositAmount);
