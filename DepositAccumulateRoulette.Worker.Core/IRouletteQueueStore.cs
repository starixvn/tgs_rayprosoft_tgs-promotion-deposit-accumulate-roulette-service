using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal interface IRouletteQueueStore
{
	string PlayerCode { get; }

	Guid PromotionId { get; }

	Guid BrandId { get; }

	IEnumerable<RouletteItem> RouletteItems { get; }

	ValueTask<RouletteEventCountInfo> GetAvailableEventAsync(CancellationToken cancellationToken = default);

	ValueTask UpdateEventUsedCountAsync(int usedCount, CancellationToken cancellationToken = default);
}
