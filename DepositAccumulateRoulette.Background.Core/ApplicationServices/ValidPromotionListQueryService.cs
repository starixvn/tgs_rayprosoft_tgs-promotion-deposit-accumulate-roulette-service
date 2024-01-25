using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.ApplicationServices;

internal class ValidPromotionListQueryService : IQueryService<ValidPromotionListQuery, PromotionInfo>
{
	private readonly IPromotionServiceClient _promotionServiceClient;

	public ValidPromotionListQueryService(
		IPromotionServiceClient promotionServiceClient) => _promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));

	public IAsyncEnumerable<PromotionInfo> QueryAsync(
		ValidPromotionListQuery query,
		CancellationToken cancellationToken = default)
	{
		var processTime = new DateTimeOffset(
			query.ProcessDate.ToDateTime(TimeOnly.MinValue),
			TimeSpan.FromHours(8D)).UtcDateTime;

		return _promotionServiceClient
			.ListPromotionsByPromotionTypeAsync("DepositAccumulateRoulette", cancellationToken)
			.Where(info => info.EventDuring.From <= processTime
				&& (info.EventDuring.To > processTime
					|| (processTime - info.EventDuring.To).Days <= 1))
			.SelectMany(info => _promotionServiceClient
				.GetPromotionRuleAsync<PromotionRule>(info.Id, cancellationToken)
				.AsTask()
				.ToAsyncEnumerable()
				.Where(rule => rule is not null)
				.Select(rule => new PromotionInfo(
					info.Id,
					info.EventDuring.From,
					info.EventDuring.To,
					info.BrandId,
					rule!.CampaignDetail)))
			.ToArrayAsync(cancellationToken)
			.AsTask()
			.ToAsyncEnumerable()
			.SelectMany(arr => arr.ToAsyncEnumerable());
	}

	internal record PromotionRule(DepositAccumulateRouletteSettings CampaignDetail);
}
