namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

public record GetPromotionDistributePlanQuery(Guid PromotionId, long ConditionHash);
