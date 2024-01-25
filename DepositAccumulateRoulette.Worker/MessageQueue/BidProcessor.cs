using Google.Protobuf;
using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.MessageQueue;

public class BidProcessor : IMessageProcessor<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>
{
	private readonly IDatabase _redisDatabase;
	private readonly ILogger<BidProcessor> _logger;

	public BidProcessor(
		IRedisConnectionManager redisConnectionManager,
		ILogger<BidProcessor> logger)
	{
		ArgumentNullException.ThrowIfNull(redisConnectionManager);

		_redisDatabase = redisConnectionManager.Database;
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = BidRequest.Parser.ParseFrom(data.Span);

		var promotionId = request.PromotionId.ToGuid();

		var redisKey = $"Promotion:DepositAccumulateRoulette:Worker:{request.PlayerCode}:{promotionId}";

		var isSet = await _redisDatabase.StringSetAsync(
				redisKey,
				Environment.MachineName,
				TimeSpan.FromMinutes(1),
				When.NotExists)
			.ConfigureAwait(false);

		var response = new BidResponse();

		if (isSet)
		{
			response.Subject = Environment.MachineName;
		}
		else
		{
			var resource = await _redisDatabase.StringGetAsync(redisKey).ConfigureAwait(false);
			response.Subject = resource;
		}

		_logger.LogInformation(
			"Subject of player({request.PlayerCode}) with promotion({promotionId}): {response.Subject}",
			request.PlayerCode,
			promotionId,
			response.Subject);

		return response.ToByteArray();
	}
}
