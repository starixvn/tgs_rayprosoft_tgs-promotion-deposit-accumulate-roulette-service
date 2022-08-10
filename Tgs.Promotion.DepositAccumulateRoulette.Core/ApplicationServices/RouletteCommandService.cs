using System.Security.Cryptography;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;

internal class RouletteCommandService : IGetService<RouletteCommand, RouletteExchangeResult>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?> _getPromotionService;
	private readonly IRouletteCountService _rouletteCountService;
	private readonly IShuffleService _shuffleService;
	private readonly IStatisticsService _statisticsService;
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;

	public RouletteCommandService(
		ITgsRepository tgsRepository,
		IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?> getPromotionService,
		IRouletteCountService rouletteCountService,
		IShuffleService shuffleService,
		IStatisticsService statisticsService,
		IPromotionServiceClient promotionServiceClient,
		IPlayerServiceClient playerServiceClient)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_getPromotionService = getPromotionService ?? throw new ArgumentNullException(nameof(getPromotionService));
		_rouletteCountService = rouletteCountService ?? throw new ArgumentNullException(nameof(rouletteCountService));
		_shuffleService = shuffleService ?? throw new ArgumentNullException(nameof(shuffleService));
		_statisticsService = statisticsService;
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
	}

	public async ValueTask<RouletteExchangeResult> GetAsync(
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

		var rouletteCountInfo = await _rouletteCountService.GetRemainingRouletteCountAsync(
			command.PromotionId,
			command.PlayerCode,
			cancellationToken).ConfigureAwait(false);

		if (rouletteCountInfo.DepositAccumulateRouletteEventId is null
			|| rouletteCountInfo.RouletteCount == 0)
		{
			return new RouletteExchangeResult(null, 0);
		}

		var (rouletteResult, remainingRouletteCount) = await ProcessRouletteAsync(
			command,
			rouletteItems,
			player.BrandId,
			rouletteCountInfo.DepositAccumulateRouletteEventId.Value,
			cancellationToken).ConfigureAwait(false);

		_ = Task.Run(async () =>
		{
			await _promotionServiceClient.UpdatePromotionUsedAmountAsync(
				command.PromotionId,
				rouletteResult.TotalAmount,
				rouletteResult.RecordId,
				default).ConfigureAwait(false);

			// 派發作業

			//await _distributeService.ExecuteAsync(new DistributeBonusCommand(
			//    command.PromotionId,
			//    command.PlayerCode,
			//    command.Date,
			//    bonus,
			//    bucket)).ConfigureAwait(false);
		});

		return new RouletteExchangeResult(
			new RouletteResult(rouletteResult.RouletteId, rouletteResult.ItemIndex),
			remainingRouletteCount);
	}

	private async ValueTask<(ProcessRouletteResult Result, int RemainingRouletteCount)> ProcessRouletteAsync(
		RouletteCommand command,
		RouletteItem[] rouletteItems,
		Guid brandId,
		Guid depositAccumulateRouletteEventId,
		CancellationToken cancellationToken)
	{
		var remainingCount = await _rouletteCountService.DecreaseRouletteCountAsync(command.PromotionId,
			command.PlayerCode, depositAccumulateRouletteEventId, cancellationToken);

		try
		{
			var itemIndex = await PlayRouletteAsync(command, rouletteItems).ConfigureAwait(false);

			var result = await ProcessRouletteResultAsync(
				command,
				rouletteItems,
				brandId,
				itemIndex,
				depositAccumulateRouletteEventId,
				cancellationToken).ConfigureAwait(false);

			return (result, remainingCount);
		}
		catch
		{
			_ = await _rouletteCountService.IncreaseRouletteCountAsync(
				command.PromotionId,
				command.PlayerCode,
				depositAccumulateRouletteEventId,
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
