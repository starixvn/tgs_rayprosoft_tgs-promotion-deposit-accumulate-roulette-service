namespace Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;

public record RouletteCommand(Guid PromotionId, string PlayerCode);
