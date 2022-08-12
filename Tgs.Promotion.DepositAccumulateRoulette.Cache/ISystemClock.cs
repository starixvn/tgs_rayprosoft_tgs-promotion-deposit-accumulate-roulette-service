namespace Tgs.Promotion.DepositAccumulateRoulette.Cache;

internal interface ISystemClock
{
	DateTimeOffset UtcNow { get; }
}
