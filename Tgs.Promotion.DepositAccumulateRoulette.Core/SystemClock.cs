namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

internal class SystemClock : ISystemClock
{
	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
