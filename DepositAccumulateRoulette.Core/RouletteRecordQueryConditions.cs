namespace Tgs.Promotion.DepositAccumulateRouletteService;

public class RouletteRecordQueryConditions
{
	public IEnumerable<string> PlayerCodes { get; init; } = Array.Empty<string>();

	public IEnumerable<Guid> PromotionIds { get; init; } = Array.Empty<Guid>();

	public (DateTimeOffset Begin, DateTimeOffset End) TimeRange { get; init; }
}
