namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

public record RouletteTaskCommand(Guid PromotionId, string PlayerCode);
