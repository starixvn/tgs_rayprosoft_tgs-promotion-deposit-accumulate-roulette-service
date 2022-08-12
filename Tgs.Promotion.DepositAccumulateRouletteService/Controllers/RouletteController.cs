using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Core;
using Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRouletteService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RouletteController : ControllerBase
{
	[HttpPost]
	public async Task<RouletteResult?> RouletteAsync(
		[FromServices] IGetService<RouletteCommand, RouletteResult> commandService,
		[FromBody] ViewModel model)
	{
		var command = new RouletteCommand(model.PromotionId, model.PlayerCode);

		var result = await commandService.GetAsync(command).ConfigureAwait(false);

		return result;
	}

	[HttpPost("event/cache/clear")]
	public async Task CacheClearAsync(
		[FromServices] IClearDepositAccumulateRouletteEventCache clearCacheService)
	{
		await clearCacheService.ClearCacheAsync(Guid.NewGuid(), "player1").ConfigureAwait(false);
	}
}
