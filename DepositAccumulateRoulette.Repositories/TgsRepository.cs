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
		=> _context.DepositAccumulateRouletteEvents
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

	public IAsyncEnumerable<Guid> GetPlayerAvailablePromotionIdsAsync(
		string playerCode,
		DateOnly queryDate)
		=> _context.DepositAccumulateRouletteEvents
			.Where(evt => evt.PlayerCode == playerCode
						  && evt.Date >= queryDate)
			.Select(evt => evt.PromotionId)
			.Distinct()
			.AsAsyncEnumerable();

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

		_ = await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

		return recordId;
	}

	public async ValueTask<RouletteRecordInfo?> GetRouletteRecordInfoAsync(
		Guid rouletteRecordId,
		CancellationToken cancellationToken = default)
		=> await _context.DepositAccumulateRouletteGameEvents
			.Where(evt => evt.Id == rouletteRecordId)
			.Select(evt => new RouletteRecordInfo(
				evt.Id,
				evt.PlayerCode,
				evt.PromotionId,
				evt.Date,
				evt.ItemIndex,
				evt.Bonus,
				evt.CreatedOn))
			.FirstOrDefaultAsync(cancellationToken)
			.ConfigureAwait(false);

	public IAsyncEnumerable<RouletteRecordInfo> GetRouletteRecordInfosAsync(Guid promotionId, string playerCode, DateOnly eventDate) => _context.DepositAccumulateRouletteGameEvents
			.Where(evt => evt.PromotionId == promotionId
				&& evt.PlayerCode == playerCode
				&& evt.Date == eventDate)
			.Select(evt => new RouletteRecordInfo(
				evt.Id,
				evt.PlayerCode,
				evt.PromotionId,
				evt.Date,
				evt.ItemIndex,
				evt.Bonus,
				evt.CreatedOn))
			.AsAsyncEnumerable();

	public IAsyncEnumerable<RouletteRecordInfo> GetRouletteRecordInfosAsync(Guid promotionId, string playerCode)
	{
		var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8D)).DateTime);
		var processDate = today.AddDays(-7);

		return _context.DepositAccumulateRouletteGameEvents
			.Where(evt => evt.PromotionId == promotionId
						  && evt.PlayerCode == playerCode
						  && evt.Date >= processDate)
			.Select(evt => new RouletteRecordInfo(
				evt.Id,
				evt.PlayerCode,
				evt.PromotionId,
				evt.Date,
				evt.ItemIndex,
				evt.Bonus,
				evt.CreatedOn))
			.AsAsyncEnumerable();
	}
}
