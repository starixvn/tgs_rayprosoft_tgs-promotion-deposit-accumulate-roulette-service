using System.Text;
using Murmur;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class DistributePlanHasher : IDistributePlanHasher
{
	public int GetHashCode(Guid depositAccumulateRouletteId, int currentRouletteCount)
	{
		var value = $"{depositAccumulateRouletteId:D},{currentRouletteCount}";

		var bytes = Encoding.UTF8.GetBytes(value);

		var algorithm = MurmurHash.Create32();
		var hash = algorithm.ComputeHash(bytes);

		return BitConverter.ToInt32(hash);
	}
}
