using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class GetPromotionDistributePlanQueryService : IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>
{
	private readonly IPromotionServiceClient _promotionServiceClient;

	public GetPromotionDistributePlanQueryService(
		IPromotionServiceClient promotionServiceClient) => _promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));

	public ValueTask<PromotionDistributePlanInfo> GetAsync(
		GetPromotionDistributePlanQuery query,
		CancellationToken cancellationToken = default)
		=> _promotionServiceClient
			.GetPromotionDistributePlanAsync(
				query.PromotionId,
				query.ConditionHash,
				cancellationToken);
}
