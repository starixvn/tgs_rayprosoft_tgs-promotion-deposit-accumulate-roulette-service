namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal interface IDistributePlanHasher
{
	int GetHashCode(Guid depositAccumulateRouletteId, int currentRouletteCount);
}
