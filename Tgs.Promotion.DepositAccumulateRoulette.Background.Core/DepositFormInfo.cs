namespace Tgs.Promotion.DepositAccumulateRoulette.Background.Core;

public record DepositFormInfo(
	Guid FormId,
	string PlayerCode,
	Guid BrandId,
	Guid PromotionId,
	decimal? ActualAmount,
	decimal Amount);
