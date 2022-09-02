using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.DepositAccumulateRoulette.Repositories.Entities;
using Tgs.Promotion.DepositAccumulateRoulette.Worker;
using Tgs.SerialNumberGenerator;

namespace Tgs.Promotion.DepositAccumulateRoulette.Repositories;

internal class TgsRepository : ITgsRepository
{
	private readonly TgsContext _context;
	private readonly ISerialNumberGenerator _serialNumberGenerator;

	public TgsRepository(
		TgsContext context,
		ISerialNumberGenerator serialNumberGenerator)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	}

	public IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		DateOnly eventDate)
	{
		return _context.DepositAccumulateRouletteEvents
			.Where(evt => evt.PlayerCode == playerCode
						  && evt.PromotionId == promotionId
						  && evt.Date >= eventDate)
			.Select(evt =>
				new DepositAccumulateRouletteEventInfo(
					evt.PlayerCode,
					evt.PromotionId,
					evt.Date,
					evt.RouletteCount,
					evt.TotalDepositAmount))
			.AsAsyncEnumerable();
	}

	public async ValueTask<int> GetRouletteUsedCountAsync(
		Guid promotionId,
		string playerCode,
		DateOnly eventDate,
		CancellationToken cancellationToken = default)
	{
		var count = await _context.DepositAccumulateRouletteGameEvents
			.Where(evt => evt.PlayerCode == playerCode
						  && evt.PromotionId == promotionId
						  && evt.Date == eventDate)
			.CountAsync(cancellationToken)
			.ConfigureAwait(false);

		return count;
	}

	public async ValueTask<Guid> AddRouletteRecordAsync(
		Guid promotionId,
		string playerCode,
		Guid brandId,
		DateOnly eventDate,
		decimal bonus,
		int itemIndex,
		CancellationToken cancellationToken = default)
	{
		var recordId = await _serialNumberGenerator.GenerateAsync(cancellationToken).ConfigureAwait(false);

		var gameEvent = new DepositAccumulateRouletteGameEvent
		{
			Id = recordId,
			Bonus = bonus,
			PromotionId = promotionId,
			PlayerCode = playerCode,
			BrandId = brandId,
			ItemIndex = itemIndex,
			Date = eventDate
		};

		_ = await _context.DepositAccumulateRouletteGameEvents
			.AddAsync(gameEvent, cancellationToken)
			.ConfigureAwait(false);

		await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

		return recordId;
	}

	public async IAsyncEnumerable<Guid> GetAvailablePromotionIdsAsync(
		string playerCode,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var date = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8D - 8D)).DateTime);

		var queryIds = await _context.DepositAccumulateRouletteEvents
			.Where(evt =>
				evt.PlayerCode == playerCode &&
				evt.Date >= date)
			.Select(evt => evt.PromotionId)
			.Distinct()
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var promotionIds = await _context.Promotions
			.Where(p => queryIds.Contains(p.Id) && p.Status == 0)
			.Select(p => p.Id)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		foreach (var promotionId in promotionIds)
			yield return promotionId;
	}
}
