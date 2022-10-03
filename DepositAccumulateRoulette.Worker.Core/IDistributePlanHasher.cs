namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal interface IDistributePlanHasher
{
	int GetHashCode(DateOnly eventDate, int currentRouletteCount);
}
