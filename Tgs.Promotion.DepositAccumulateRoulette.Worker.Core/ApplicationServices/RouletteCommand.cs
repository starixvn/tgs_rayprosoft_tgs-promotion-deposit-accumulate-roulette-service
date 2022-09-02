namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

public record RouletteCommand(Guid PromotionId, string PlayerCode);
