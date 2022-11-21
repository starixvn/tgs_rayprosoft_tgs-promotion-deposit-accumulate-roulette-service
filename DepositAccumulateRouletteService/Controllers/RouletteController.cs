using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.DepositAccumulateRouletteService.ViewModels;

namespace Tgs.Promotion.DepositAccumulateRouletteService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RouletteController : ControllerBase
{
	[HttpGet("records/{playerCode}/{promotionId}", Name = "QueryRouletteRecordsByPlayerCodeAndPromotionId")]
	public async IAsyncEnumerable<RouletteRecordView> QueryRouletteRecordsAsync(
		[FromServices] IDepositAccumulateRouletteWorkerClient client,
		string playerCode,
		Guid promotionId)
	{
		var records = client.QueryRouletteRecordsAsync(
			promotionId,
			playerCode,
			HttpContext.RequestAborted);

		await foreach (var info in records)
			yield return new RouletteRecordView
			{
				RouletteId = info.RouletteId,
				PlayerCode = info.PlayerCode,
				PromotionId = info.PromotionId,
				EventDate = info.EventDate,
				ItemIndex = info.ItemIndex,
				CreatedOn = info.CreatedOn
			};
	}

	[HttpGet("records/{brandId}", Name = "QueryRouletteRecordsByBrandId")]
	public async IAsyncEnumerable<RouletteRecordView> QueryRouletteRecordsAsync(
		[FromServices] IDepositAccumulateRouletteWorkerClient client,
		Guid brandId,
		[FromQuery] RouletteRecordsQueryViewModel conditions,
		[FromQuery] PagingViewModel paging)
	{
		var records = client
			.QueryRouletteRecordsAsync(
				brandId,
				new RouletteRecordQueryConditions
				{
					PlayerCodes = conditions.PlayerCodes,
					PromotionIds = conditions.PromotionIds,
					TimeRange = (conditions.Begin, conditions.End)
				},
				new Paging
				{
					IsDesc = paging.IsDesc,
					Page = paging.Page,
					ShowAll = paging.ShowAll,
					Rows = paging.Rows,
					SortBy = paging.SortBy
				},
				HttpContext.RequestAborted);

		await foreach (var info in records)
			yield return new RouletteRecordView
			{
				RouletteId = info.RouletteId,
				PlayerCode = info.PlayerCode,
				PromotionId = info.PromotionId,
				EventDate = info.EventDate,
				ItemIndex = info.ItemIndex,
				CreatedOn = info.CreatedOn
			};
	}

	[HttpGet("record/{brandId}/{recordId}", Name = "GetRouletteRecord")]
	public async ValueTask<RouletteRecordView?> GetRouletteRecordAsync(
		[FromServices] IDepositAccumulateRouletteWorkerClient client,
		Guid brandId,
		Guid recordId)
	{
		var recordInfo = await client
			.GetRouletteRecordAsync(brandId, recordId, HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return recordInfo is null
			? null
			: new RouletteRecordView
			{
				RouletteId = recordInfo.RouletteId,
				PlayerCode = recordInfo.PlayerCode,
				PromotionId = recordInfo.PromotionId,
				EventDate = recordInfo.EventDate,
				ItemIndex = recordInfo.ItemIndex,
				CreatedOn = recordInfo.CreatedOn
			};
	}
}
