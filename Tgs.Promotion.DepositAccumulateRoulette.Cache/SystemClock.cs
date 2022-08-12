namespace Tgs.Promotion.DepositAccumulateRoulette.Cache;

internal class SystemClock : ISystemClock
{
	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
