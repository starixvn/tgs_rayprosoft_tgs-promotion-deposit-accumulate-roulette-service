using Microsoft.AspNetCore.Mvc;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HashController : ControllerBase
{
	[HttpGet("{date}/{count}")]
	public ValueTask<int> GetHashAsync(
		DateOnly date,
		int count,
		[FromServices] IDistributePlanHasher hasher)
	{
		var hash = hasher.GetHashCode(date, count);

		return ValueTask.FromResult(hash);
	}
}
