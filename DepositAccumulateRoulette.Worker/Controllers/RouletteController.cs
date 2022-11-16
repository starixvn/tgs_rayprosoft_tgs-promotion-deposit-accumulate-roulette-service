using Microsoft.AspNetCore.Mvc;
using Tgs.Paging;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RouletteController : ControllerBase
{
	[HttpGet("records/{playerCode}/{promotionId}", Name = "QueryRouletteRecordsByPlayerAndPromotion")]
	public async IAsyncEnumerable<RouletteRecordViewModel> QueryRouletteRecordsAsync(
		[FromServices] IQueryService<RouletteRecordsQuery, RouletteRecordInfo> queryService,
		string playerCode,
		Guid promotionId)
	{
		var rouletteRecordInfos = queryService
			.QueryAsync(
				new RouletteRecordsQuery(promotionId, playerCode));

		await foreach (var info in rouletteRecordInfos.WithCancellation(HttpContext.RequestAborted))
			yield return new RouletteRecordViewModel
			{
				RouletteId = info.RecordId,
				PlayerCode = info.PlayerCode,
				PromotionId = info.PromotionId,
				EventDate = info.EventDate,
				ItemIndex = info.ItemIndex,
				CreatedOn = info.CreatedOn
			};
	}

	[HttpGet("records/{brandId}", Name = "QueryRouletteRecordsByConditions")]
	public async IAsyncEnumerable<RouletteRecordViewModel> QueryRouletteRecordsAsync(
		[FromServices] IQueryService<RouletteRecordsConditionQuery, RouletteRecordInfo> queryService,
		Guid brandId,
		[FromQuery] RouletteRecordsQueryViewModel conditions,
		[FromQuery] PagingViewModel pagingModel)
	{
		var query = new RouletteRecordsConditionQuery(
			brandId,
			conditions.PlayerCodes,
			conditions.PromotionIds,
			conditions.Begin,
			conditions.End);

		var paging = new Paging<RouletteRecordInfo>();

		if (!string.IsNullOrEmpty(pagingModel.SortBy))
			paging.SortByPropertyName(pagingModel.SortBy, pagingModel.IsDesc ?? false);

		paging.RowCount = pagingModel.Rows ?? 5000;
		paging.Page = pagingModel.Page ?? 1;

		var result = queryService.QueryAsync(query, HttpContext.RequestAborted);

		var records = await result
			.ApplyOrderBy(paging)
			.ApplyPaging(paging)
			.ToArrayAsync(HttpContext.RequestAborted)
			.ConfigureAwait(false);

		foreach (var info in records)
			yield return new RouletteRecordViewModel
			{
				RouletteId = info.RecordId,
				PlayerCode = info.PlayerCode,
				PromotionId = info.PromotionId,
				EventDate = info.EventDate,
				ItemIndex = info.ItemIndex,
				CreatedOn = info.CreatedOn
			};
	}
}
