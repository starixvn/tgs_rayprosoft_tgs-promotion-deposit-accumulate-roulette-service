namespace Tgs.Promotion.DepositAccumulateRouletteServices.ApplicationServices;

internal record RouletteUsedCountQuery(
	Guid PromotionId,
	string PlayerCode,
	Guid DepositAccumulateRouletteEventId);
