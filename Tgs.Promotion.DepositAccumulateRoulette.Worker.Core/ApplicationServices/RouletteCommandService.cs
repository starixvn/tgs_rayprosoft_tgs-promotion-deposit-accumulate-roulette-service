using System.Linq;
using System.Security.Cryptography;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class RouletteCommandService : IGetService<RouletteCommand, RouletteResult>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?> _getPromotionService;
	private readonly IQueryService<GetRouletteEventCountQuery, RouletteEventCountInfo> _eventCountQueryService;
	private readonly IShuffleService _shuffleService;
	private readonly IStatisticsService _statisticsService;
	private readonly ICommandService<DistributeBonusCommand> _distributeService;
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;

	public RouletteCommandService(
		ITgsRepository tgsRepository,
		IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?> getPromotionService,
		IQueryService<GetRouletteEventCountQuery, RouletteEventCountInfo> eventCountQueryService,
		IShuffleService shuffleService,
		IStatisticsService statisticsService,
		ICommandService<DistributeBonusCommand> distributeService,
		IPromotionServiceClient promotionServiceClient,
		IPlayerServiceClient playerServiceClient)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_getPromotionService = getPromotionService ?? throw new ArgumentNullException(nameof(getPromotionService));
		_eventCountQueryService = eventCountQueryService ?? throw new ArgumentNullException(nameof(eventCountQueryService));
		_shuffleService = shuffleService ?? throw new ArgumentNullException(nameof(shuffleService));
		_statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
		_distributeService = distributeService ?? throw new ArgumentNullException(nameof(distributeService));
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
	}

	public async ValueTask<RouletteResult> GetAsync(
		RouletteCommand command, CancellationToken cancellationToken = default)
	{
		var player = await _playerServiceClient.GetPlayerInfoAsync(command.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		var promotionSetting = await _getPromotionService.GetAsync(
			new GetDepositAccumulateRoulettePromotionQuery(command.PromotionId),
			cancellationToken).ConfigureAwait(false);

		var rouletteItems = promotionSetting!.CampaignDetail.RouletteItems.ToArray();

		var eventCountInfos = await _eventCountQueryService.QueryAsync(
				new GetRouletteEventCountQuery(command.PromotionId, command.PlayerCode),
				cancellationToken)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		if (!eventCountInfos.Any())
			return new RouletteResult(null, -1);

		var useInfo = GetUseEventCountInfo(eventCountInfos);

		if (useInfo is null)
			return new RouletteResult(null, -1);

		var (success, decreasedCount) = await _statisticsService.DecreaseRouletteCountAsync(
			useInfo.PromotionId,
			useInfo.PlayerCode,
			useInfo.Date,
			cancellationToken);

		if (!success)
		{
			return new RouletteResult(null, -1);
		}

		var afterTotalCount =
			eventCountInfos.Except(new[] { useInfo }).Sum(info => info.AvailableCount) + decreasedCount;

		var countInfo = new RouletteCountInfo(
			useInfo,
			decreasedCount,
			afterTotalCount);

		var rouletteResult = await ProcessRouletteAsync(
			command,
			rouletteItems,
			player!.BrandId,
			countInfo,
			cancellationToken).ConfigureAwait(false);

		_ = Task.Run(async () =>
		{
			await _promotionServiceClient.UpdatePromotionUsedAmountAsync(
				command.PromotionId,
				rouletteResult.TotalAmount,
				rouletteResult.RecordId).ConfigureAwait(false);

			await _distributeService.ExecuteAsync(new DistributeBonusCommand(
					command.PromotionId,
					command.PlayerCode,
					countInfo.UseEvent.Date,
					countInfo.EventUsedCount,
					rouletteResult.Bonus,
					rouletteResult.Bucket)).ConfigureAwait(false);
		});

		return new RouletteResult(
			rouletteResult.RouletteId,
			rouletteResult.ItemIndex);
	}

	private static RouletteEventCountInfo? GetUseEventCountInfo(
		IEnumerable<RouletteEventCountInfo> infos)
		=> infos.OrderBy(info => info.Date)
			.FirstOrDefault(info => info.AvailableCount > 0);

	private async ValueTask<ProcessRouletteResult> ProcessRouletteAsync(
		RouletteCommand command,
		RouletteItem[] rouletteItems,
		Guid brandId,
		RouletteCountInfo countInfo,
		CancellationToken cancellationToken)
	{
		try
		{
			var itemIndex = await PlayRouletteAsync(command, rouletteItems).ConfigureAwait(false);

			var result = await ProcessRouletteResultAsync(
				command,
				rouletteItems,
				brandId,
				itemIndex,
				countInfo.UseEvent.Date,
				cancellationToken).ConfigureAwait(false);

			return result;
		}
		catch
		{
			_ = await _statisticsService.IncreaseRouletteCountAsync(
				command.PromotionId,
				command.PlayerCode,
				countInfo.UseEvent.Date,
				cancellationToken);

			throw;
		}
	}

	private async ValueTask<ProcessRouletteResult> ProcessRouletteResultAsync(
		RouletteCommand command,
		RouletteItem[] rouletteItems,
		Guid brandId,
		int itemIndex,
		DateOnly eventDate,
		CancellationToken cancellationToken)
	{
		var rouletteItem = rouletteItems[itemIndex];
		var distributeAmount = rouletteItem.Bonus;

		var totalAmount = await _statisticsService
			.IncrementTotalDistributeAmountAsync(
				command.PromotionId,
				distributeAmount,
				cancellationToken)
			.ConfigureAwait(false);

		var recordId = await _tgsRepository.AddRouletteRecordAsync(
				command.PromotionId,
				command.PlayerCode,
				brandId,
				eventDate,
				distributeAmount,
				itemIndex,
				cancellationToken)
			.ConfigureAwait(false);

		var bucket = rouletteItem.Bonus * rouletteItem.TurnoverRate;

		return new ProcessRouletteResult(
			recordId,
			totalAmount,
			rouletteItem.Bonus,
			bucket,
			rouletteItem.Id,
			itemIndex);
	}

	private async ValueTask<int> PlayRouletteAsync(
		RouletteCommand command,
		IEnumerable<RouletteItem> rouletteItems)
	{
		var deck = await _shuffleService
			.ShuffleAsync(command.PromotionId, rouletteItems)
			.ConfigureAwait(false);

		var deckIndex = RandomNumberGenerator.GetInt32(deck.Length);

		return deck[deckIndex];
	}

	internal record ProcessRouletteResult(
		Guid RecordId,
		decimal TotalAmount,
		decimal Bonus,
		decimal Bucket,
		Guid RouletteId,
		int ItemIndex);

	internal record RouletteCountInfo(
		RouletteEventCountInfo UseEvent,
		int AfterEventCount,
		int AfterTotalCount)
	{
		public int EventUsedCount => UseEvent.DistributeCount - AfterEventCount;
	}
}
