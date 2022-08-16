namespace Tgs.DepositAccumulateRoulette.Clients;

public record RouletteSetting(Guid PromotionId, IEnumerable<RouletteItem> Items);
