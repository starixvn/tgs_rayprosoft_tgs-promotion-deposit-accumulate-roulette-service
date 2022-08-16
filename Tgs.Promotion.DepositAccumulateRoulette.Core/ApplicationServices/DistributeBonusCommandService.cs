using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRouletteServices.ApplicationServices;

internal class DistributeBonusCommandService : ICommandService<DistributeBonusCommand>
{
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo> _getDistributePlanService;
	private readonly IDistributePlanHasher _hasher;

	public DistributeBonusCommandService(
		IPromotionServiceClient promotionServiceClient,
		IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo> getDistributePlanService,
		IDistributePlanHasher hasher)
	{
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_getDistributePlanService = getDistributePlanService ?? throw new ArgumentNullException(nameof(getDistributePlanService));
		_hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
	}

	public async ValueTask ExecuteAsync(
		DistributeBonusCommand command,
		CancellationToken cancellationToken = default)
	{
		var hash = _hasher.GetHashCode(command.DepositAccumulateRouletteEventId, command.CurrentRouletteCount);

		var plan = await _getDistributePlanService.GetAsync(
			new GetPromotionDistributePlanQuery(
				command.PromotionId,
				hash), cancellationToken);

		_ = await _promotionServiceClient.DistributePromotionBonusAsync(
			plan.Id,
			command.PlayerCode,
			command.Bonus,
			command.Bucket,
			null,
			cancellationToken).ConfigureAwait(false);
	}
}
