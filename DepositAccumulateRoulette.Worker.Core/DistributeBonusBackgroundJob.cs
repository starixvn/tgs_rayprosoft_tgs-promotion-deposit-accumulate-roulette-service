using Hangfire;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class DistributeBonusBackgroundJob
{
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly ICommandService<DistributeBonusCommand> _distributeService;

	public DistributeBonusBackgroundJob(
		IPromotionServiceClient promotionServiceClient,
		ICommandService<DistributeBonusCommand> distributeService)
	{
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_distributeService = distributeService ?? throw new ArgumentNullException(nameof(distributeService));
	}

	[AutomaticRetry(Attempts = 0)]
	public async Task ExecuteAsync(DistributeBonusScheduleInfo scheduleInfo)
	{
		await _promotionServiceClient.UpdatePromotionUsedAmountAsync(
			scheduleInfo.PromotionId,
			scheduleInfo.PlayResult.TotalAmount,
			scheduleInfo.PlayResult.RecordId).ConfigureAwait(false);

		await _distributeService.ExecuteAsync(new DistributeBonusCommand(
			scheduleInfo.PromotionId,
			scheduleInfo.PlayerCode,
			scheduleInfo.Hash,
			scheduleInfo.PlayResult.Bonus,
			scheduleInfo.PlayResult.Bucket)).ConfigureAwait(false);
	}
}
