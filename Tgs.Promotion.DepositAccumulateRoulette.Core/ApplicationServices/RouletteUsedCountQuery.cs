namespace Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;

internal record RouletteUsedCountQuery(
	Guid PromotionId,
	string PlayerCode,
	Guid DepositAccumulateRouletteEventId);
