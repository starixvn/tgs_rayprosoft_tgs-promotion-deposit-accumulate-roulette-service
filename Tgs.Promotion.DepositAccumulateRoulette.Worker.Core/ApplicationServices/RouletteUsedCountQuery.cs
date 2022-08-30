namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal record RouletteUsedCountQuery(
	Guid PromotionId,
	string PlayerCode,
	DateOnly EventDate);
