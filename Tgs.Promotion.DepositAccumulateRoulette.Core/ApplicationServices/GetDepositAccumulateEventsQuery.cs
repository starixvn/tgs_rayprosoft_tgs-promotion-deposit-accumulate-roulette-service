namespace Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;

public record GetDepositAccumulateEventsQuery(
	Guid PromotionId,
	string PlayerCode,
	DateOnly QueryDate);
