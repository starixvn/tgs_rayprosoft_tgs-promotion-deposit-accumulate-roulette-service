using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

public interface IShuffleService
{
	ValueTask<int[]> ShuffleAsync(
		Guid promotionId,
		IEnumerable<RouletteItem> items);
}
