using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRouletteService.ApplicationServices;

internal class GetDepositAccumulateRoulettePromotionQueryService
	: IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotionInfo?>
{
	private readonly IPromotionServiceClient _promotionServiceClient;

	public GetDepositAccumulateRoulettePromotionQueryService(
		IPromotionServiceClient promotionServiceClient)
	{
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
	}

	public async ValueTask<DepositAccumulateRoulettePromotionInfo?> GetAsync(GetDepositAccumulateRoulettePromotionQuery query,
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

		return rule is null
			? throw new InvalidOperationException("DepositAccumulateRoulette setting is null")
			: new DepositAccumulateRoulettePromotionInfo(
			mainInfo.Id,
			mainInfo.EventDuring,
			mainInfo.PromotionLimitAmount,
			mainInfo.IsAutoSent,
			rule.CampaignDetail);
	}

	internal record PromotionRule(DepositAccumulateRouletteSettings CampaignDetail);
}
