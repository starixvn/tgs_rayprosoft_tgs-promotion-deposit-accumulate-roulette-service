namespace Tgs.DepositAccumulateRoulette.Clients;

public class RouletteRecordQuery
{
	public IEnumerable<string> PlayerCodes { get; init; } = Array.Empty<string>();

	public IEnumerable<Guid> PromotionIds { get; init; } = Array.Empty<Guid>();

	public DateTimeOffset BeginTime { get; init; }

	public DateTimeOffset EndTime { get; init; }
}
