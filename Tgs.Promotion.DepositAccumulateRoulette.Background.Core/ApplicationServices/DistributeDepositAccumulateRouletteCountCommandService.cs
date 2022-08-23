using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.ApplicationServices;

internal class DistributeDepositAccumulateRouletteCountCommandService : ICommandService<DistributeDepositAccumulateRouletteCountCommand>
{
	private readonly IBackgroundRepository _backgroundRepository;
	private readonly DepositAccumulateRouletteSerializer _serializer;

	public DistributeDepositAccumulateRouletteCountCommandService(
		IBackgroundRepository backgroundRepository,
		DepositAccumulateRouletteSerializer serializer)
	{
		_backgroundRepository = backgroundRepository ?? throw new ArgumentNullException(nameof(backgroundRepository));
		_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
	}

	public async ValueTask ExecuteAsync(
		DistributeDepositAccumulateRouletteCountCommand command,
		CancellationToken cancellationToken = default)
	{
		var promotions = await _backgroundRepository
			.GetValidDepositAccumulateRoulettePromotionsAsync(command.ProcessDate, cancellationToken)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		if (promotions.Length == 0)
			return;

		var brandIds = promotions.Select(p => p.BrandId).Distinct();

		var depositForms = await _backgroundRepository
			.GetApprovedDepositFormsAsync(brandIds, command.ProcessDate, cancellationToken)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		if (depositForms.Length == 0)
			return;

		var depositAccumulateEvents = new List<DepositAccumulateRouletteEventAddInfo>();

		foreach (var promotion in promotions)
		{
			var settings = _serializer.Deserialize<DepositAccumulateRouletteSettings>(promotion.CampaignDetail);

			if (settings?.DepositItems is null)
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
				var reachedItem = settings.DepositItems
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
			return;

		await _backgroundRepository
			.AddDepositAccumulateRouletteEventsAsync(command.ProcessDate, depositAccumulateEvents, cancellationToken)
			.ConfigureAwait(false);
	}
}
