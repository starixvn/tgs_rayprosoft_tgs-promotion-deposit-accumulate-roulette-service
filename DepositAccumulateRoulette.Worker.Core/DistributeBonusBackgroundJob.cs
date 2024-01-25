using Hangfire;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class DistributeBonusBackgroundJob(
	IPromotionServiceClient promotionServiceClient,
	ITgsRepository tgsRepository,
	IDistributePlanHasher hasher,
	ICommandService<DistributeBonusCommand> distributeService,
	ILogger<DistributeBonusBackgroundJob> logger)
{
	private readonly IPromotionServiceClient _promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	private readonly IDistributePlanHasher _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
	private readonly ICommandService<DistributeBonusCommand> _distributeService = distributeService ?? throw new ArgumentNullException(nameof(distributeService));
	private readonly ILogger<DistributeBonusBackgroundJob> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
