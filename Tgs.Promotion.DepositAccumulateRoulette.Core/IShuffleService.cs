using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRouletteServices;

public interface IShuffleService
{
	ValueTask<int[]> ShuffleAsync(
		Guid promotionId,
		IEnumerable<RouletteItem> items);
}
