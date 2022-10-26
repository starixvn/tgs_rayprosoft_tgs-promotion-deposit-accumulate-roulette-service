using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DistributeBonusController : ControllerBase
{
	[HttpPost("manuel/{rouletteRecordId}", Name = "ManuelDistributeBonus")]
	public ValueTask ManuelDistributeBonusAsync(
		Guid rouletteRecordId,
		[FromServices] ICommandService<ManuelDistributeBonusCommand> commandService)
		=> commandService.ExecuteAsync(new ManuelDistributeBonusCommand(rouletteRecordId));
}
