using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public interface IShuffleService
{
	ValueTask<int[]> ShuffleAsync(
		Guid promotionId,
		IEnumerable<RouletteItem> items);
}
