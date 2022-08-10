namespace Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;

internal record GetDepositAccumulateEventsQuery(
	Guid PromotionId,
	string PlayerCode,
	DateOnly QueryDate);
