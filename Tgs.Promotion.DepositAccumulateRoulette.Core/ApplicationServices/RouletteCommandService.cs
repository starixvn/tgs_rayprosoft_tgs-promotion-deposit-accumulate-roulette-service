using System.Security.Cryptography;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRouletteServices.ApplicationServices;

internal class RouletteCommandService : IGetService<RouletteCommand, RouletteResult>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?> _getPromotionService;
	private readonly IRouletteCountService _rouletteCountService;
	private readonly IShuffleService _shuffleService;
	private readonly IStatisticsService _statisticsService;
	private readonly ICommandService<DistributeBonusCommand> _distributeService;
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;

	public RouletteCommandService(
		ITgsRepository tgsRepository,
		IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?> getPromotionService,
		IRouletteCountService rouletteCountService,
		IShuffleService shuffleService,
		IStatisticsService statisticsService,
		ICommandService<DistributeBonusCommand> distributeService,
		IPromotionServiceClient promotionServiceClient,
		IPlayerServiceClient playerServiceClient)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_getPromotionService = getPromotionService ?? throw new ArgumentNullException(nameof(getPromotionService));
		_rouletteCountService = rouletteCountService ?? throw new ArgumentNullException(nameof(rouletteCountService));
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

		if (player is null)
			throw new InvalidOperationException($"Can't find player with code {command.PlayerCode}");

		var promotionSetting = await _getPromotionService.GetAsync(
			new GetDepositAccumulateRoulettePromotionQuery(command.PromotionId),
			cancellationToken).ConfigureAwait(false);

		if (promotionSetting is null)
			throw new InvalidOperationException($"Promotion {command.PromotionId} does not exist.");

		var rouletteItems = promotionSetting.CampaignDetail.RouletteItems?.ToArray() ?? Array.Empty<RouletteItem>();

		if (rouletteItems.Length == 0)
			throw new InvalidOperationException($"Promotion {command.PromotionId} does not have any roulette items.");

		var countInfo = await _rouletteCountService.DecreaseRouletteCountAsync(
			command.PromotionId,
			command.PlayerCode,
			cancellationToken);

		if (countInfo.UseEvent is null || countInfo.BeforeTotalCount <= 0)
		{
			return new RouletteResult(null, -1, 0);
		}

		var rouletteResult = await ProcessRouletteAsync(
			command,
			rouletteItems,
			player.BrandId,
			countInfo,
			cancellationToken).ConfigureAwait(false);

		_ = Task.Run(async () =>
		{
			await _promotionServiceClient.UpdatePromotionUsedAmountAsync(
				command.PromotionId,
				rouletteResult.TotalAmount,
				rouletteResult.RecordId,
				default).ConfigureAwait(false);

			await _distributeService.ExecuteAsync(new DistributeBonusCommand(
					command.PromotionId,
					command.PlayerCode,
					countInfo.UseEvent.EventId,
					countInfo.BeforeEventCount,
					rouletteResult.Bonus,
					rouletteResult.Bucket),
				default).ConfigureAwait(false);
		});

		return new RouletteResult(
			rouletteResult.RouletteId,
			rouletteResult.ItemIndex,
			countInfo.AfterTotalCount);
	}

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
				countInfo.UseEvent!.EventId,
				cancellationToken).ConfigureAwait(false);

			return result;
		}
		catch
		{
			await _rouletteCountService.IncreaseRouletteCountAsync(
				command.PromotionId,
				command.PlayerCode,
				countInfo.UseEvent!.Date,
				cancellationToken);

			throw;
		}
	}

	private async ValueTask<ProcessRouletteResult> ProcessRouletteResultAsync(
		RouletteCommand command,
		RouletteItem[] rouletteItems,
		Guid brandId,
		int itemIndex,
		Guid depositAccumulateRouletteEventId,
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
				depositAccumulateRouletteEventId,
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
		RouletteItem[] rouletteItems)
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
