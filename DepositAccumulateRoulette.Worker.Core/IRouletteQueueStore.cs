using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal interface IRouletteQueueStore
{
	string PlayerCode { get; }

	Guid PromotionId { get; }

	Guid BrandId { get; }

	IEnumerable<RouletteItem> RouletteItems { get; }

	ValueTask<RouletteEventCountInfo> GetAvailableEventAsync(
		IServiceCoordinator serviceCoordinator,
		CancellationToken cancellationToken = default);

	ValueTask UpdateEventUsedCountAsync(int usedCount);
}
