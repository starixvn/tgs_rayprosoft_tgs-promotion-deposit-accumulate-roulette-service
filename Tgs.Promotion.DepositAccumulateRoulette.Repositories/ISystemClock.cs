namespace Tgs.Promotion.DepositAccumulateRoulette.Repositories;

internal interface ISystemClock
{
	DateTimeOffset UtcNow { get; }
}
