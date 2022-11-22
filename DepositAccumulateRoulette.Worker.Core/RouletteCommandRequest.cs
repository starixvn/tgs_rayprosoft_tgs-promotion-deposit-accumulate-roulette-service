namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal record RouletteCommandRequest(Guid PromotionId, string PlayerCode);
