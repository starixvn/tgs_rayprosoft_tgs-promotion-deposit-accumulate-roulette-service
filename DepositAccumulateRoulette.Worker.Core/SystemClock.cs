namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class SystemClock : ISystemClock
{
	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
