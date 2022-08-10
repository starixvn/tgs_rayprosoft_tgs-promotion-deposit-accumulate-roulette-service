namespace Tgs.Promotion.DepositAccumulateRoulette.Client;

public record RouletteSetting(Guid PromotionId, IEnumerable<RouletteItem> Items);