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

	public async IAsyncEnumerable<DepositAccumulateRouletteEventInfo> GetDepositAccumulateRouletteEventInfosAsync(
		Guid promotionId,
		string playerCode,
		DateOnly eventDate,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var events = await _context.DepositAccumulateRouletteEvents
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
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		foreach (var info in events)
			yield return info;
	}

	public async IAsyncEnumerable<Guid> GetPlayerRouletteAvailablePromotionIdsAsync(
		string playerCode,
		DateOnly queryDate,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var promotionIds = await _context.DepositAccumulateRouletteEvents
			.Where(evt => evt.PlayerCode == playerCode
						  && evt.Date >= queryDate)
			.Select(evt => evt.PromotionId)
			.Distinct()
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		foreach (var id in promotionIds)
			yield return id;
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

	public async ValueTask<Guid> CreateRouletteRecordAsync(
		CreateRouletteRecordInfo createRecordInfo,
		CancellationToken cancellationToken = default)
	{
		var recordId = await _serialNumberGenerator.GenerateAsync(cancellationToken).ConfigureAwait(false);

		var gameEvent = new DepositAccumulateRouletteGameEvent
		{
			Id = recordId,
			PromotionId = createRecordInfo.PromotionId,
			PlayerCode = createRecordInfo.PlayerCode,
			BrandId = createRecordInfo.BrandId,
			ItemIndex = createRecordInfo.ItemIndex,
			Date = createRecordInfo.EventDate,
			Bonus = createRecordInfo.BonusAmount,
			Bucket = createRecordInfo.BucketAmount,
			RouletteCount = createRecordInfo.RouletteCount
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
				evt.BrandId,
				evt.Date,
				evt.ItemIndex,
				evt.Bonus,
				evt.Bucket,
				evt.RouletteCount,
				evt.CreatedOn))
			.FirstOrDefaultAsync(cancellationToken)
			.ConfigureAwait(false);

	public async IAsyncEnumerable<RouletteRecordInfo> QueryRouletteRecordListAsync(
		Guid promotionId,
		string playerCode,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8D)).DateTime);
		var processDate = today.AddDays(-7);

		var recordInfos = await _context.DepositAccumulateRouletteGameEvents
			.Where(evt => evt.PromotionId == promotionId
						  && evt.PlayerCode == playerCode
						  && evt.Date >= processDate)
			.Select(evt => new RouletteRecordInfo(
				evt.Id,
				evt.PlayerCode,
				evt.PromotionId,
				evt.BrandId,
				evt.Date,
				evt.ItemIndex,
				evt.Bonus,
				evt.Bucket,
				evt.RouletteCount,
				evt.CreatedOn))
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		foreach (var info in recordInfos)
			yield return info;
	}

	public async IAsyncEnumerable<RouletteRecordInfo> QueryRouletteRecordListAsync(
		Guid brandId,
		IEnumerable<string> playerCodes,
		IEnumerable<Guid> promotionIds,
		DateTime beginTime,
		DateTime endTime,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8D)).DateTime);
		var processDate = today.AddDays(-90);

		var query = _context.DepositAccumulateRouletteGameEvents
			.Where(evt =>
				evt.BrandId == brandId
				&& evt.Date >= processDate
				&& evt.CreatedOn >= beginTime
				&& evt.CreatedOn < endTime);

		if (promotionIds.Any())
			query = query.Where(evt => promotionIds.Contains(evt.PromotionId));

		if (playerCodes.Any())
			query = query.Where(evt => playerCodes.Contains(evt.PlayerCode));

		var recordInfos = await query
			.Select(evt => new RouletteRecordInfo(
				evt.Id,
				evt.PlayerCode,
				evt.PromotionId,
				evt.BrandId,
				evt.Date,
				evt.ItemIndex,
				evt.Bonus,
				evt.Bucket,
				evt.RouletteCount,
				evt.CreatedOn))
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		foreach (var info in recordInfos)
			yield return info;
	}
}
