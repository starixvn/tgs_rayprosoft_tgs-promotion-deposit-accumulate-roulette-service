using Microsoft.Extensions.Logging;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class ManuelDistributeBonusCommandService : ICommandService<ManuelDistributeBonusCommand>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly IDepositAccumulateRouletteServiceClient _rouletteServiceClient;
	private readonly IDistributePlanHasher _hasher;
	private readonly ICommandService<DistributeBonusCommand> _distributeService;
	private readonly ILogger<ManuelDistributeBonusCommandService> _logger;

	public ManuelDistributeBonusCommandService(
		ITgsRepository tgsRepository,
		IDepositAccumulateRouletteServiceClient rouletteServiceClient,
		IDistributePlanHasher hasher,
		ICommandService<DistributeBonusCommand> distributeService,
		ILogger<ManuelDistributeBonusCommandService> logger)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_rouletteServiceClient = rouletteServiceClient ?? throw new ArgumentNullException(nameof(rouletteServiceClient));
		_hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
		_distributeService = distributeService ?? throw new ArgumentNullException(nameof(distributeService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask ExecuteAsync(
		ManuelDistributeBonusCommand command,
		CancellationToken cancellationToken = default)
	{
		var rouletteRecordInfo = await _tgsRepository
			.GetRouletteRecordInfoAsync(
				command.RouletteRecordId,
				cancellationToken)
			.ConfigureAwait(false);

		if (rouletteRecordInfo == null)
			return;

		var promotion = await _rouletteServiceClient
				.GetDepositAccumulateRoulettePromotionAsync(rouletteRecordInfo.PromotionId,
					cancellationToken)
				.ConfigureAwait(false);

		if (promotion == null)
			return;

		var turnoverRate = promotion.CampaignDetail.RouletteItems
			.ElementAt(rouletteRecordInfo.ItemIndex)
			.TurnoverRate;

		var bucket = rouletteRecordInfo.BonusAmount * turnoverRate;

		var allRecords = await _tgsRepository.GetRouletteRecordInfosAsync(
				rouletteRecordInfo.PromotionId,
				rouletteRecordInfo.PlayerCode,
				rouletteRecordInfo.EventDate)
			.OrderBy(r => r.CreatedOn)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var usedCount = Array.IndexOf(allRecords, rouletteRecordInfo) + 1;

		var hash = _hasher.GetHashCode(rouletteRecordInfo.EventDate, usedCount);

		_logger.LogInformation(
			"Hash generate with Date: {date}, Count: {count}; Hash: {hash}",
			rouletteRecordInfo.EventDate, usedCount, hash);

		await _distributeService
			.ExecuteAsync(
				new DistributeBonusCommand(
					rouletteRecordInfo.PromotionId,
					rouletteRecordInfo.PlayerCode,
					hash,
					rouletteRecordInfo.BonusAmount,
					bucket),
				cancellationToken).ConfigureAwait(false);
	}
}
