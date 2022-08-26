using Google.Protobuf;
using StackExchange.Redis;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;

namespace Tgs.Promotion.DepositAccumulateRouletteService.Services;

internal class RouletteHandleService : IRouletteHandleService
{
	private readonly IDatabase _redisDatabase;
	private readonly IMessageSender _messageSender;
	private readonly ILogger<RouletteHandleService> _logger;

	public RouletteHandleService(
		IDatabase redisDatabase,
		IMessageSender messageSender,
		ILogger<RouletteHandleService> logger)
	{
		_redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask<RouletteResult> RequestAsync(
		RouletteTaskRequest request,
		CancellationToken cancellationToken)
	{
		var machineName = await GetProcessSubjectAsync(request.PlayerCode, cancellationToken).ConfigureAwait(false);

		var data = await _messageSender.RequestAsync(
			$"tgs.promotion.depositaccumulateroulette.worker.{machineName}.roulette",
			request.ToByteArray(),
			cancellationToken)
			.ConfigureAwait(false);

		var response = RouletteTaskResponse.Parser.ParseFrom(data.Span);

		return new RouletteResult(response.Id, response.ItemIndex, response.RemainingCount);
	}

	private async ValueTask<string> GetProcessSubjectAsync(string playerCode, CancellationToken cancellationToken)
	{
		var redisKey = $"Promotion:DepositAccumulateRoulette:Worker:{playerCode}";

		var subject = await _redisDatabase.StringGetAsync(redisKey).ConfigureAwait(false);

		if (subject.HasValue)
			return subject!;

		_logger.LogInformation("Start Bid");

		var bidRequest = new BidRequest
		{
			PlayerCode = playerCode
		};

		var responseData = await _messageSender.RequestAsync(
				"tgs.promotion.depositaccumulateroulette.bid",
				bidRequest.ToByteArray(),
				cancellationToken)
			.ConfigureAwait(false);

		var response = BidResponse.Parser.ParseFrom(responseData.Span);

		_logger.LogInformation($"Subject after bid is {response.Subject}");

		return response.Subject;
	}
}
