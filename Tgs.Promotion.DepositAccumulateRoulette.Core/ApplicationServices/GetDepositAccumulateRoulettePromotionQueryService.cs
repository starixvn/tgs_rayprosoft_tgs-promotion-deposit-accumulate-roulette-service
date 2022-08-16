using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRouletteServices.ApplicationServices;

internal class GetDepositAccumulateRoulettePromotionQueryService
	: IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>
{
	private readonly IPromotionServiceClient _promotionServiceClient;

	public GetDepositAccumulateRoulettePromotionQueryService(
		IPromotionServiceClient promotionServiceClient)
	{
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
	}

	public async ValueTask<DepositAccumulateRoulettePromotion?> GetAsync(GetDepositAccumulateRoulettePromotionQuery query,
		CancellationToken cancellationToken = default)
	{
		var mainInfo = await _promotionServiceClient
			.GetPromotionMainInfoAsync(query.PromotionId, cancellationToken)
			.ConfigureAwait(false);

		if (mainInfo is null)
			return null;

		if (mainInfo.PromotionType != "DepositAccumulateRoulette")
			throw new InvalidOperationException($"Promotion with id {query.PromotionId} is not a DepositAccumulateRoulette promotion");

		var rule = await _promotionServiceClient
			.GetPromotionRuleAsync<PromotionRule>(query.PromotionId, cancellationToken)
			.ConfigureAwait(false);

		if (rule is null)
			throw new InvalidOperationException("DepositAccumulateRoulette setting is null");

		return new DepositAccumulateRoulettePromotion(
			mainInfo.Id,
			mainInfo.PromotionLimitAmount,
			rule.CompaignDetail);
	}

	internal class PromotionRule
	{
		public DepositAccumulateRouletteSettings CompaignDetail { get; init; } = default!;
	}
}
