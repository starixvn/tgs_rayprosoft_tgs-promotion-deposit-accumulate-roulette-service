using Google.Protobuf;
using StackExchange.Redis;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.MessageQueue;

public class BidProcessor : IMessageProcessor
{
	private readonly IDatabase _redisDatabase;
	private readonly ILogger<BidProcessor> _logger;

	public BidProcessor(
		IRedisConnectionManager redisConnectionManager,
		ILogger<BidProcessor> logger)
	{
		if (redisConnectionManager is null)
			throw new ArgumentNullException(nameof(redisConnectionManager));

		_redisDatabase = redisConnectionManager.Database;
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = BidRequest.Parser.ParseFrom(data.Span);

		var redisKey = $"Promotion:DepositAccumulateRoulette:Worker:{request.PlayerCode}";

		var isSet = await _redisDatabase.StringSetAsync(
				redisKey,
				Environment.MachineName,
				TimeSpan.FromMinutes(1),
				When.NotExists)
			.ConfigureAwait(false);

		var response = new BidResponse();

		if (isSet)
			response.Subject = Environment.MachineName;
		else
		{
			var subject = await _redisDatabase.StringGetAsync(redisKey).ConfigureAwait(false);
			response.Subject = subject;
		}

		_logger.LogInformation($"Subject of player({request.PlayerCode}): {response.Subject}");

		return response.ToByteArray();
	}
}
