namespace Tgs.Promotion.DepositAccumulateRoulette.Repositories;

internal class SystemClock : ISystemClock
{
	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
