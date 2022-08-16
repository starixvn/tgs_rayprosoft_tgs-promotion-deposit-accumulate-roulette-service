namespace Tgs.Promotion.DepositAccumulateRouletteServices;

internal interface IDistributePlanHasher
{
	int GetHashCode(Guid depositAccumulateRouletteId, int currentRouletteCount);
}
