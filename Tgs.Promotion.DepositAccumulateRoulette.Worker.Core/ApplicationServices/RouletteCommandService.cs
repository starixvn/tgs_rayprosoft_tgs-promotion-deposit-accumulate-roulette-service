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

		var useInfo = await GetUseEventCountInfoAsync(
			command.PromotionId,
			command.PlayerCode,
			cancellationToken)
			.ConfigureAwait(false);

		if (useInfo is null)
			return new RouletteResult(null, -1);

		var (success, decreasedCount) = await _statisticsService.DecreaseRouletteCountAsync(
			useInfo.PromotionId,
			useInfo.PlayerCode,
			useInfo.Date,
			cancellationToken);

		if (!success)
			return new RouletteResult(null, -1);

		var rouletteResult = await ProcessRouletteAsync(
			command,
			rouletteItems,
			player!.BrandId,
			useInfo.Date,
			cancellationToken).ConfigureAwait(false);

		var playedCount = useInfo.DistributeCount - decreasedCount;

		_ = Task.Run(async () =>
		{
			await _promotionServiceClient.UpdatePromotionUsedAmountAsync(
				command.PromotionId,
				rouletteResult.TotalAmount,
				rouletteResult.RecordId).ConfigureAwait(false);

			await _distributeService.ExecuteAsync(new DistributeBonusCommand(
					command.PromotionId,
					command.PlayerCode,
					useInfo.Date,
					playedCount,
					rouletteResult.Bonus,
					rouletteResult.Bucket)).ConfigureAwait(false);
		});

		return new RouletteResult(
			rouletteResult.RouletteId,
			rouletteResult.ItemIndex);
	}

	private async ValueTask<RouletteEventCountInfo?> GetUseEventCountInfoAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken)
	{
		var eventCountInfos = await _eventCountQueryService
			.QueryAsync(
				new GetRouletteEventCountQuery(promotionId, playerCode),
				cancellationToken)
			.OrderBy(info => info.Date)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		return eventCountInfos.FirstOrDefault(info => info.AvailableCount > 0);
	}

	private async ValueTask<ProcessRouletteResult> ProcessRouletteAsync(
		RouletteCommand command,
		RouletteItem[] rouletteItems,
		Guid brandId,
		DateOnly date,
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
				date,
				cancellationToken).ConfigureAwait(false);

			return result;
		}
		catch
		{
			_ = await _statisticsService.IncreaseRouletteCountAsync(
				command.PromotionId,
				command.PlayerCode,
				date,
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
}
