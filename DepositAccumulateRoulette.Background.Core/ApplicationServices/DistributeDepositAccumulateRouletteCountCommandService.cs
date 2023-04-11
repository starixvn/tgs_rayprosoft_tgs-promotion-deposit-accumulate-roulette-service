using System.Runtime.CompilerServices;
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

		var depositAccumulateEvents = new List<DepositAccumulateRouletteEventAddInfo>();

		foreach (var promotion in promotions)
		{
			if (!promotion.CampaignDetail.DepositItems.Any())
				continue;

			var depositAccumulateItems =
				(from info in depositForms
				 where info.PromotionId == promotion.Id
				 group info by info.PlayerCode
					into g
				 select new
				 {
					 PlayerCode = g.Key,
					 DepositAmount = g.ToArray().Sum(form => form.ActualAmount ?? form.Amount)
				 }).ToArray();

			if (depositAccumulateItems.Length == 0)
				continue;

			foreach (var item in depositAccumulateItems)
			{
				var reachedItem = promotion.CampaignDetail.DepositItems
					.Where(set => set.AccumulateAmount <= item.DepositAmount)
					.MaxBy(set => set.AccumulateAmount);

				if (reachedItem is not null)
				{
					depositAccumulateEvents.Add(
						new DepositAccumulateRouletteEventAddInfo(
							item.PlayerCode,
							promotion.BrandId,
							promotion.Id,
							reachedItem.RouletteCount,
							item.DepositAmount));
				}
			}
		}

		if (depositAccumulateEvents.Count == 0)
			yield break;

		await _backgroundRepository
			.AddDepositAccumulateRouletteEventsAsync(command.ProcessDate, depositAccumulateEvents, cancellationToken)
			.ConfigureAwait(false);

		foreach (var addEvt in depositAccumulateEvents)
			yield return addEvt;
	}
}
