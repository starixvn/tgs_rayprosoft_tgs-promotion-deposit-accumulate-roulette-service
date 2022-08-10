namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

internal interface IDistributePlanHasher
{
	int GetHashCode(Guid depositAccumulateRouletteId, int currentRouletteCount);
}
