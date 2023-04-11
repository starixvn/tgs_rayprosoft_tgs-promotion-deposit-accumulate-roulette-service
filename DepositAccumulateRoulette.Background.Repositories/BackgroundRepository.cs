using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.DepositAccumulateRoulette.Background.Repositories.Entities;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.Repositories;

internal class BackgroundRepository : IBackgroundRepository
{
	private readonly TgsContext _context;

	public BackgroundRepository(
		TgsContext context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
	}

	public IAsyncEnumerable<DepositFormInfo> GetApprovedDepositFormsAsync(
		IEnumerable<Guid> brandIds,
		DateOnly processDate,
		CancellationToken cancellationToken = default)
	{
		var (fromDate, toDate) = (processDate.AddDays(-1), processDate);
		var (from, to) = (
			new DateTimeOffset(fromDate.Year, fromDate.Month, fromDate.Day, 0, 0, 0, TimeSpan.FromHours(8D)).UtcDateTime,
			new DateTimeOffset(toDate.Year, toDate.Month, toDate.Day, 0, 0, 0, TimeSpan.FromHours(8D)).UtcDateTime);

		var depositForms = _context.DepositForms
			.Where(form => form.UpdatedOn >= from
						   && form.UpdatedOn < to
						   && form.Status >= 8000
						   && brandIds.Contains(form.BrandId)
						   && form.PromotionId != null)
			.Select(form => new DepositFormInfo(
				form.Id,
				form.PlayerCode,
				form.BrandId,
				form.PromotionId!.Value,
				form.ActualAmount,
				form.Amount))
			.AsAsyncEnumerable();

		return depositForms;
	}

	public async ValueTask AddDepositAccumulateRouletteEventsAsync(
		DateOnly processDate,
		IEnumerable<DepositAccumulateRouletteEventAddInfo> events,
		CancellationToken cancellationToken = default)
	{
		var playerCodes = events.Select(e => e.PlayerCode).Distinct().ToArray();

		var existData = await _context.DepositAccumulateRouletteEvents
			.Where(evt => playerCodes.Contains(evt.PlayerCode)
						  && evt.Date == processDate)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		foreach (var evt in events)
		{
			var exist = existData.Any(x =>
				x.PlayerCode == evt.PlayerCode && x.PromotionId == evt.PromotionId);

			if (exist)
				continue;

			var eventData = new DepositAccumulateRouletteEvent
			{
				PlayerCode = evt.PlayerCode,
				BrandId = evt.BrandId,
				Date = processDate,
				PromotionId = evt.PromotionId,
				RouletteCount = evt.RouletteCount,
				TotalDepositAmount = evt.TotalDepositAmount
			};

			_ = await _context.DepositAccumulateRouletteEvents.AddAsync(eventData, cancellationToken)
				.ConfigureAwait(false);
		}

		_ = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}
}
