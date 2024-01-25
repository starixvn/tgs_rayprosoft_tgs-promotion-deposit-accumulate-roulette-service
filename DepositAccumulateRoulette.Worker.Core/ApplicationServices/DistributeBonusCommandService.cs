using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class DistributeBonusCommandService(
	IPromotionServiceClient promotionServiceClient,
	IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo> getDistributePlanService) : ICommandService<DistributeBonusCommand>
{
	private readonly IPromotionServiceClient _promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
	private readonly IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo> _getDistributePlanService = getDistributePlanService ?? throw new ArgumentNullException(nameof(getDistributePlanService));

	public async ValueTask ExecuteAsync(
		DistributeBonusCommand command,
		CancellationToken cancellationToken = default)
	{
		var plan = await _getDistributePlanService.GetAsync(
			new GetPromotionDistributePlanQuery(
				command.PromotionId,
				command.DistributePlanHash), cancellationToken);

		_ = await _promotionServiceClient.DistributePromotionBonusAsync(
			plan.Id,
			command.PlayerCode,
			command.BonusAmount,
			command.BucketAmount,
			distributor: null,
			command.RouletteRecordId,
			cancellationToken: cancellationToken).ConfigureAwait(false);
	}
}
