using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Background.Protos;
using Tgs.WebApi.Toolkits;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SchedulerController : ControllerBase
{
    [HttpPost("calculate", Name = "QueueDepositAccumulateCalculationJob")]
    [SuccessStatusCode(202)]
    public void QueueDepositAccumulateCalculationJob(
        [FromServices] IMessageSender messageSender)
    {
        var currentDate = DateOnly.FromDateTime(DateTimeOffset.Now
            .ToOffset(TimeSpan.FromHours(8D))
            .DateTime);

        var request = new ProcessRequest
        {
            ProcessDate = currentDate
        };

        _ = messageSender
            .SendAsync(
                "tgs.promotion.depositaccumulateroulette.background.calculate",
                request.ToByteArray())
            .AsTask();
    }

    [HttpPost("calculate/{processDate}", Name = "QueueDepositAccumulateCalculationJobWithProcessDate")]
    [SuccessStatusCode(202)]
    public void QueueDepositAccumulateCalculationJob(
        [FromServices] IMessageSender messageSender,
        DateOnly processDate)
    {
        var request = new ProcessRequest
        {
            ProcessDate = processDate

        };

        _ = messageSender
            .SendAsync(
                "tgs.promotion.depositaccumulateroulette.background.calculate",
                request.ToByteArray())
            .AsTask();
    }
}