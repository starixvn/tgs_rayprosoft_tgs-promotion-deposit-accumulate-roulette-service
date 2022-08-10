using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

public interface ITgsRepository
{
	IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		DateOnly eventDate);

	ValueTask<int> GetRouletteUsedCountAsync(
		Guid promotionId,
		string playerCode,
		Guid depositAccumulateRouletteEventId,
		CancellationToken cancellationToken = default);

	ValueTask<Guid> AddRouletteRecordAsync(
		Guid promotionId,
		string playerCode,
		Guid brandId,
		Guid depositAccumulateRouletteEventId,
		decimal bonus,
		int itemIndex,
		CancellationToken cancellationToken = default);
}
