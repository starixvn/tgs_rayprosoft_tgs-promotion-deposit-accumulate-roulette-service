using System.Collections.Immutable;
using System.Security.Cryptography;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class PlayRouletteCommandService(
	IShuffleService shuffleService,
	IStatisticsService statisticsService,
	ITgsRepository tgsRepository) : IGetService<PlayRouletteCommand, PlayRouletteResult>
{
	private readonly IShuffleService _shuffleService = shuffleService ?? throw new ArgumentNullException(nameof(shuffleService));
	private readonly IStatisticsService _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));

	public async ValueTask<PlayRouletteResult> GetAsync(
		PlayRouletteCommand command,
		CancellationToken cancellationToken = default)
	{
		var (success, usedCount) = await _statisticsService.IncreaseRouletteUsedCountAsync(
			command.EventInfo.PromotionId,
			command.EventInfo.PlayerCode,
			command.EventInfo.Date,
			cancellationToken);

		if (!success)
			return new PlayRouletteResult(null, 0);

		if (command.EventInfo.DistributeCount < usedCount)
			return new PlayRouletteResult(null, usedCount);

		var rouletteItems = command.RouletteItems.ToImmutableArray();

		var itemIndex = await PlayRouletteAsync(command.EventInfo.PromotionId, rouletteItems).ConfigureAwait(false);

		var result = await ProcessRouletteResultAsync(
			command.EventInfo,
			rouletteItems,
			command.BrandId,
			itemIndex,
			usedCount,
			cancellationToken).ConfigureAwait(false);

		return new PlayRouletteResult(result, usedCount);
	}

	private async ValueTask<PlayRouletteResult.RouletteResult> ProcessRouletteResultAsync(
		RouletteEventCountInfo eventInfo,
		IReadOnlyList<RouletteItem> rouletteItems,
		Guid brandId,
		int itemIndex,
		int usedCount,
		CancellationToken cancellationToken)
	{
		var rouletteItem = rouletteItems[itemIndex];
		var distributeAmount = rouletteItem.Bonus;

		var bucket = rouletteItem.Bonus * rouletteItem.TurnoverRate;

		var totalAmount = await _statisticsService
			.IncrementTotalDistributeAmountAsync(
				eventInfo.PromotionId,
				distributeAmount,
				cancellationToken)
			.ConfigureAwait(false);

		var recordId = await _tgsRepository.CreateRouletteRecordAsync(
				new CreateRouletteRecordInfo(
					eventInfo.PromotionId,
					eventInfo.PlayerCode,
					brandId,
					eventInfo.Date,
					usedCount,
					distributeAmount,
					bucket,
					itemIndex),
				cancellationToken)
			.ConfigureAwait(false);

		return new PlayRouletteResult.RouletteResult(
			recordId,
			totalAmount,
			rouletteItem.Bonus,
			bucket,
			rouletteItem.Id,
			itemIndex);
	}

	private async ValueTask<int> PlayRouletteAsync(
		Guid promotionId,
		IEnumerable<RouletteItem> rouletteItems)
	{
		var deck = await _shuffleService
			.ShuffleAsync(promotionId, rouletteItems)
			.ConfigureAwait(false);

		var deckIndex = RandomNumberGenerator.GetInt32(deck.Length);

		return deck[deckIndex];
	}
}
