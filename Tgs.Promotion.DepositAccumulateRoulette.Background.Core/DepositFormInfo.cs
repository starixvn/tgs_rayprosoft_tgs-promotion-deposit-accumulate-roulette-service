namespace Tgs.Promotion.DepositAccumulateRoulette.Background;

public record DepositFormInfo(
	Guid FormId,
	string PlayerCode,
	Guid BrandId,
	Guid PromotionId,
	decimal? ActualAmount,
	decimal Amount);
