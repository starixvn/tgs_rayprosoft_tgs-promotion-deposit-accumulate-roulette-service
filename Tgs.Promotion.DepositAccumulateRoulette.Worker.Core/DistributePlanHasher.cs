using System.Text;
using Murmur;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class DistributePlanHasher : IDistributePlanHasher
{
	public int GetHashCode(DateOnly eventDate, int currentRouletteCount)
	{
		var value = $"{eventDate:O},{currentRouletteCount}";

		var bytes = Encoding.UTF8.GetBytes(value);

		var algorithm = MurmurHash.Create32();
		var hash = algorithm.ComputeHash(bytes);

		return BitConverter.ToInt32(hash);
	}
}
