using Google.Protobuf;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Background.Protos;
using Tgs.WebApi.Toolkits;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SchedulerController : ControllerBase
{
	[HttpPost("distribute/roulette/count", Name = "QueueProcessDistributeDepositAccumulateRouletteCountJob")]
	[SuccessStatusCode(202)]
	public void QueueProcessDistributeDepositAccumulateRouletteCountCalculationJob()
	{
		var currentDate = DateOnly.FromDateTime(DateTimeOffset.Now
			.ToOffset(TimeSpan.FromHours(8D))
			.DateTime);

		_ = BackgroundJob.Enqueue<ScheduleBackgroundJob>(job =>
			job.ExecuteAsync(currentDate, "tgs.promotion.depositaccumulateroulette.background.roulettecount.distribute"));
	}

	[HttpPost("distribute/roulette/count/{processDate}", Name = "QueueProcessDistributeDepositAccumulateRouletteCountWithProcessDate")]
	[SuccessStatusCode(202)]
	public void QueueProcessDistributeDepositAccumulateRouletteCountCalculationWithProcessDate(
		DateOnly processDate)
	{
		_ = BackgroundJob.Enqueue<ScheduleBackgroundJob>(job =>
			job.ExecuteAsync(processDate, "tgs.promotion.depositaccumulateroulette.background.roulettecount.distribute"));
	}
}
