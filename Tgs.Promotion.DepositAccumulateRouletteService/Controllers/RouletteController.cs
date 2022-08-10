using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRouletteService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RouletteController : ControllerBase
{
	[HttpPost]
	public async Task<RouletteExchangeResult?> RouletteAsync(
		[FromServices] IGetService<RouletteCommand, RouletteExchangeResult> commandService,
		[FromBody] ViewModel model)
	{
		var command = new RouletteCommand(model.PromotionId, model.PlayerCode);

		var result = await commandService.GetAsync(command).ConfigureAwait(false);

		return result;
	}
}
