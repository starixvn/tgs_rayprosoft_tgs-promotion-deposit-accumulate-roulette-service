using Google.Protobuf;
using Tgs.Promotion.DepositAccumulateRoulette.Background.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background;

public class ScheduleBackgroundJob
{
	private readonly IMessageSender _messageSender;

	public ScheduleBackgroundJob(IMessageSender messageSender)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public Task ExecuteAsync(DateOnly processDate, string subject)
	{
		var request = new ProcessRequest
		{
			ProcessDate = processDate
		};

		return _messageSender
			.SendAsync(subject, request.ToByteArray())
			.AsTask();
	}
}
