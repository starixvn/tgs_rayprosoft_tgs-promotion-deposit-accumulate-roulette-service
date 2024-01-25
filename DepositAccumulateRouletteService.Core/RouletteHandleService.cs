using Google.Protobuf;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.DepositAccumulateRouletteService;

internal class RouletteHandleService(
	IDatabase redisDatabase,
	IMessageSender messageSender,
	ILogger<RouletteHandleService> logger) : IRouletteHandleService
{
	private readonly IDatabase _redisDatabase = redisDatabase ?? throw new ArgumentNullException(nameof(redisDatabase));
	private readonly IMessageSender _messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	private readonly ILogger<RouletteHandleService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async ValueTask<RouletteResult> RequestAsync(
		RouletteTaskRequest request,
		CancellationToken cancellationToken)
	{
		var machineName = await GetProcessSubjectAsync(request.PlayerCode, request.PromotionId, cancellationToken).ConfigureAwait(false);

		var data = await _messageSender.RequestAsync<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(
			$"tgs.promotion.depositaccumulateroulette.worker.{machineName}.roulette",
			request.ToByteArray(),
			cancellationToken)
			.ConfigureAwait(false);

		var response = RouletteTaskResponse.Parser.ParseFrom(data.Span);

		return new RouletteResult(response.Id, response.ItemIndex);
	}

	private async ValueTask<string> GetProcessSubjectAsync(string playerCode, Guid promotionId, CancellationToken cancellationToken)
	{
		var redisKey = $"Promotion:DepositAccumulateRoulette:Worker:{playerCode}:{promotionId}";

		var subject = await _redisDatabase.StringGetAsync(redisKey).ConfigureAwait(false);

		if (subject.HasValue)
			return subject!;

		_logger.LogInformation("Start Bid");

		var bidRequest = new BidRequest
		{
			PromotionId = promotionId,
			PlayerCode = playerCode
		};

		var responseData = await _messageSender.RequestAsync<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(
				"tgs.promotion.depositaccumulateroulette.bid",
				bidRequest.ToByteArray(),
				cancellationToken)
			.ConfigureAwait(false);

		var response = BidResponse.Parser.ParseFrom(responseData.Span);

		_logger.LogInformation("Subject after bid is {response.Subject}", response.Subject);

		return response.Subject;
	}
}
