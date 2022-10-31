using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RouletteController : ControllerBase
{
	[HttpGet("records/{playerCode}/{promotionId}")]
	public async IAsyncEnumerable<RouletteRecordViewModel> GetRouletteRecordAsync(
		[FromServices] IQueryService<GetRouletteRecordsQuery, RouletteRecordInfo> queryService,
		string playerCode,
		Guid promotionId)
	{
		var rouletteRecordInfos = queryService
			.QueryAsync(
				new GetRouletteRecordsQuery(promotionId, playerCode));

		await foreach (var info in rouletteRecordInfos.WithCancellation(HttpContext.RequestAborted))
			yield return new RouletteRecordViewModel
			{
				RouletteId = info.RouletteId,
				EventDate = info.EventDate,
				ItemIndex = info.ItemIndex,
				CreatedOn = info.CreatedOn
			};
	}
}
