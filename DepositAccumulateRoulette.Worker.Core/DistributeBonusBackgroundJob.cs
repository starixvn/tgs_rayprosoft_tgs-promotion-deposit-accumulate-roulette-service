using Hangfire;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class DistributeBonusBackgroundJob
{
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly ITgsRepository _tgsRepository;
	private readonly IDistributePlanHasher _hasher;
	private readonly ICommandService<DistributeBonusCommand> _distributeService;
	private readonly ILogger<DistributeBonusBackgroundJob> _logger;

	public DistributeBonusBackgroundJob(
		IPromotionServiceClient promotionServiceClient,
		ITgsRepository tgsRepository,
		IDistributePlanHasher hasher,
		ICommandService<DistributeBonusCommand> distributeService,
		ILogger<DistributeBonusBackgroundJob> logger)
	{
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
		_distributeService = distributeService ?? throw new ArgumentNullException(nameof(distributeService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	[AutomaticRetry(Attempts = 0)]
	public async Task ExecuteAsync(DistributeBonusScheduleInfo scheduleInfo)
	{
		var recordInfo = (await _tgsRepository.GetRouletteRecordInfoAsync(scheduleInfo.RouletteRecordId)
			.ConfigureAwait(false))!;

		var distributePlanHash = _hasher.GetHashCode(recordInfo.EventDate, recordInfo.RouletteCount);

		_logger.LogInformation(
		"Hash generate with Date: {date}, Count: {count}; Hash: {hash}",
		recordInfo.EventDate, recordInfo.RouletteCount, distributePlanHash);

		await _promotionServiceClient.UpdatePromotionUsedAmountAsync(
			recordInfo.PromotionId,
			scheduleInfo.TotalDistributeAmount,
			recordInfo.RecordId).ConfigureAwait(false);

		await _distributeService
			.ExecuteAsync(
				new DistributeBonusCommand(
					recordInfo.PromotionId,
					recordInfo.PlayerCode,
					distributePlanHash,
					recordInfo.BonusAmount,
					recordInfo.BucketAmount,
					recordInfo.RecordId))
			.ConfigureAwait(false);
	}
}
