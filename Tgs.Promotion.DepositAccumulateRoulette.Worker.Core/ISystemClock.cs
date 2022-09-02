namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal interface ISystemClock
{
	DateTimeOffset UtcNow { get; }
}
