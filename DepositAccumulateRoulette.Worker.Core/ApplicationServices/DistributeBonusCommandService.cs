using Microsoft.Extensions.Logging;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class DistributeBonusCommandService : ICommandService<DistributeBonusCommand>
{
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo> _getDistributePlanService;

	public DistributeBonusCommandService(
		IPromotionServiceClient promotionServiceClient,
		IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo> getDistributePlanService)
	{
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_getDistributePlanService = getDistributePlanService ?? throw new ArgumentNullException(nameof(getDistributePlanService));
	}

	public async ValueTask ExecuteAsync(
		DistributeBonusCommand command,
		CancellationToken cancellationToken = default)
	{
		var plan = await _getDistributePlanService.GetAsync(
			new GetPromotionDistributePlanQuery(
				command.PromotionId,
				command.Hash), cancellationToken);

		_ = await _promotionServiceClient.DistributePromotionBonusAsync(
			plan.Id,
			command.PlayerCode,
			command.Bonus,
			command.Bucket,
			null,
			cancellationToken).ConfigureAwait(false);
	}
}
