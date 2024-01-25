using System.Runtime.CompilerServices;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.ApplicationServices;

internal class DistributeDepositAccumulateRouletteCountCommandService
	: IQueryService<DistributeDepositAccumulateRouletteCountCommand, DepositAccumulateRouletteEventAddInfo>
{
	private readonly IBackgroundRepository _backgroundRepository;
	private readonly IQueryService<ValidPromotionListQuery, PromotionInfo> _promotionListQueryService;

	public DistributeDepositAccumulateRouletteCountCommandService(
		IBackgroundRepository backgroundRepository,
		IQueryService<ValidPromotionListQuery, PromotionInfo> promotionListQueryService)
	{
		_backgroundRepository = backgroundRepository ?? throw new ArgumentNullException(nameof(backgroundRepository));
		_promotionListQueryService = promotionListQueryService ?? throw new ArgumentNullException(nameof(promotionListQueryService));
	}

	public async IAsyncEnumerable<DepositAccumulateRouletteEventAddInfo> QueryAsync(
		DistributeDepositAccumulateRouletteCountCommand command,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var promotions = await _promotionListQueryService.QueryAsync(
				new ValidPromotionListQuery(command.ProcessDate),
				cancellationToken)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		if (promotions.Length == 0)
			yield break;

		var brandIds = promotions.Select(p => p.BrandId).Distinct();

		var depositForms = await _backgroundRepository
			.GetApprovedDepositFormsAsync(brandIds, command.ProcessDate, cancellationToken)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		if (depositForms.Length == 0)
			yield break;

		var addEvents = new List<DepositAccumulateRouletteEventAddInfo>();

		foreach (var promotion in promotions.Where(p => p.CampaignDetail.DepositItems.Any()))
		{
			var playerDepositItems =
				from info in depositForms
				where info.PromotionId == promotion.Id
				group info by info.PlayerCode
				into g
				select new
				{
					PlayerCode = g.Key,
					TotalDepositAmount = g.Sum(form =>
					{
						var amount = form.ActualAmount ?? form.Amount;
						return Math.Ceiling(amount);
					})
				};

			var addItems =
				from item in playerDepositItems
				let rouletteCount = GetDistributeRouletteCount(promotion.CampaignDetail.DepositItems, item.TotalDepositAmount)
				where rouletteCount > 0
				select new DepositAccumulateRouletteEventAddInfo(
					item.PlayerCode,
					promotion.BrandId,
					promotion.Id,
					rouletteCount,
					item.TotalDepositAmount);

			addEvents.AddRange(addItems);
		}

		if (addEvents.Count == 0)
			yield break;

		await _backgroundRepository
			.AddDepositAccumulateRouletteEventsAsync(command.ProcessDate, addEvents, cancellationToken)
			.ConfigureAwait(false);

		foreach (var evt in addEvents)
			yield return evt;
	}

	private static int GetDistributeRouletteCount(
		IEnumerable<DepositAccumulateItem> depositSettings,
		decimal totalDepositAmount)
	{
		var reachedItem = depositSettings
			.Where(set => set.AccumulateAmount <= totalDepositAmount)
			.MaxBy(set => set.AccumulateAmount);

		return reachedItem?.RouletteCount ?? 0;
	}
}
