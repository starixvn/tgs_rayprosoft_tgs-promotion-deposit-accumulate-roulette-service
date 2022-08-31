namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal record RouletteTaskQueueRequest(Guid PromotionId, string PlayerCode);
