namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

internal interface ISystemClock
{
	DateTimeOffset UtcNow { get; }
}
