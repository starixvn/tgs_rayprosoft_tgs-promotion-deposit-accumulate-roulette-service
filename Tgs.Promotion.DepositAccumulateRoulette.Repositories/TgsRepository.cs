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
			.Where(evt => evt.PromotionId == promotionId
						  && evt.Date >= eventDate)
			.Select(evt =>
				new DepositAccumulateRouletteEventInfo(
					evt.Id,
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
		Guid depositAccumulateRouletteEventId,
		CancellationToken cancellationToken = default)
	{
		var count = await _context.DepositAccumulateRouletteGameEvents
			.Where(evt => evt.PromotionId == promotionId
						  && evt.PlayerCode == playerCode
						  && evt.DepositAccumulateRouletteEventId == depositAccumulateRouletteEventId)
			.CountAsync(cancellationToken)
			.ConfigureAwait(false);

		return count;
	}

	public async ValueTask<Guid> AddRouletteRecordAsync(
		Guid promotionId,
		string playerCode, Guid brandId,
		Guid depositAccumulateRouletteEventId,
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
			DepositAccumulateRouletteEventId = depositAccumulateRouletteEventId
		};

		_ = await _context.DepositAccumulateRouletteGameEvents
			.AddAsync(gameEvent, cancellationToken)
			.ConfigureAwait(false);

		await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

		return recordId;
	}

	public IAsyncEnumerable<Guid> GetAvailablePromotionIdsAsync(
		Guid brandId,
		string playerCode,
		CancellationToken cancellationToken)
	{
		var date = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8D - 8D)).DateTime);

		return _context.DepositAccumulateRouletteEvents
			.Where(evt =>
					evt.BrandId == brandId &&
					evt.PlayerCode == playerCode &&
					evt.Date >= date)
			.Select(evt => evt.PromotionId)
			.Distinct()
			.ToAsyncEnumerable();
	}
}
