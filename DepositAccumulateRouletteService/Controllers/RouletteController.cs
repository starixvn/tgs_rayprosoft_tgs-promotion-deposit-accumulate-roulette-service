using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.DepositAccumulateRouletteService.ViewModels;

namespace Tgs.Promotion.DepositAccumulateRouletteService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RouletteController : ControllerBase
{
	[HttpGet("records/{playerCode}/{promotionId}")]
	public async IAsyncEnumerable<RouletteRecordView> GetRouletteRecordAsync(
		[FromServices] IDepositAccumulateRouletteWorkerClient client,
		string playerCode,
		Guid promotionId)
	{
		var records = client
			.GetRouletteRecordsAsync(promotionId, playerCode, HttpContext.RequestAborted);

		await foreach (var info in records)
			yield return new RouletteRecordView
			{
				RouletteId = info.RouletteId,
				EventDate = info.EventDate,
				ItemIndex = info.ItemIndex,
				CreatedOn = info.CreatedOn
			};
	}
}
