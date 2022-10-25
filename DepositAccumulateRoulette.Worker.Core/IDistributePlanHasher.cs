namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public interface IDistributePlanHasher
{
	int GetHashCode(DateOnly eventDate, int currentRouletteCount);
}
