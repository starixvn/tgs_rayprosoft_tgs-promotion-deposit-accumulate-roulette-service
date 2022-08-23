using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.DepositAccumulateRoulette.Background.Repositories.Entities;
using Tgs.SerialNumberGenerator;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.Repositories;

internal class BackgroundRepository : IBackgroundRepository
{
	private readonly TgsContext _context;
	private readonly ISerialNumberGenerator _serialNumberGenerator;

	public BackgroundRepository(
		TgsContext context,
		ISerialNumberGenerator serialNumberGenerator)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	}

	public IAsyncEnumerable<PromotionInfo> GetValidDepositAccumulateRoulettePromotionsAsync(
		DateOnly processDate,
		CancellationToken cancellationToken = default)
	{
		var processTime = new DateTimeOffset(
			processDate.Year, processDate.Month, processDate.Day, 0, 0, 0, TimeSpan.FromHours(8D)).UtcDateTime;

		var promotions = _context.Promotions
			.Where(p => p.Status == 0
						&& p.PromotionType == "DepositAccumulateRoulette"
						&& p.From <= processTime
						&& (p.To > processTime || (processTime - p.To).Days <= 1))
			.Select(p => new PromotionInfo(p.Id, p.From, p.To, p.BrandId, p.CampaignDetail))
			.AsAsyncEnumerable();

		return promotions;
	}

	public IAsyncEnumerable<DepositFormInfo> GetApprovedDepositFormsAsync(
		IEnumerable<Guid> brandIds,
		DateOnly processDate,
		CancellationToken cancellationToken = default)
	{
		var (from, to) = (processDate.AddDays(-1), processDate);
		var queryTimeRange = (
			From: new DateTimeOffset(from.Year, from.Month, from.Day, 0, 0, 0, TimeSpan.FromHours(8D)).UtcDateTime,
			To: new DateTimeOffset(to.Year, to.Month, to.Day, 0, 0, 0, TimeSpan.FromHours(8D)).UtcDateTime);

		var depositForms = _context.DepositForms
			.Where(form => form.UpdatedOn >= queryTimeRange.From
						   && form.UpdatedOn < queryTimeRange.To
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
		var existData = await _context.DepositAccumulateRouletteEvents
			.Where(evt => evt.Date == processDate)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		foreach (var evt in events)
		{
			var exist = existData.Any(x =>
				x.PlayerCode == evt.PlayerCode && x.PromotionId == evt.PromotionId);

			if (exist)
				continue;

			var eventId = await _serialNumberGenerator.GenerateAsync(cancellationToken).ConfigureAwait(false);

			var eventData = new DepositAccumulateRouletteEvent
			{
				Id = eventId,
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
