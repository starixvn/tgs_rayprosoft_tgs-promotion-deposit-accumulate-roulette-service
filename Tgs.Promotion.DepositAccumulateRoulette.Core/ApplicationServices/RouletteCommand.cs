namespace Tgs.Promotion.DepositAccumulateRouletteServices.ApplicationServices;

public record RouletteCommand(Guid PromotionId, string PlayerCode);
