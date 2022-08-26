using Google.Protobuf;
using StackExchange.Redis;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.MessageQueue;

public class RouletteTaskProcessor : IMessageProcessor
{
	private readonly RouletteTaskQueueManager _rouletteTaskQueueManager;
	private readonly IDatabase _redisDatabase;

	public RouletteTaskProcessor(
		RouletteTaskQueueManager rouletteTaskQueueManager,
		IRedisConnectionManager redisConnectionManager)
	{
		_rouletteTaskQueueManager = rouletteTaskQueueManager ?? throw new ArgumentNullException(nameof(rouletteTaskQueueManager));

		if (redisConnectionManager is null)
			throw new ArgumentNullException(nameof(redisConnectionManager));

		_redisDatabase = redisConnectionManager.Database;
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
	{
		var request = RouletteTaskRequest.Parser.ParseFrom(data.Span);

		var redisKey = $"Promotion:DepositAccumulateRoulette:Worker:{request.PlayerCode}";

		var subject = await _redisDatabase.StringGetAsync(redisKey).ConfigureAwait(false);

		if (!subject.HasValue || (string)subject! != Environment.MachineName)
			throw new InvalidOperationException($"This machine ({Environment.MachineName}) not match {subject}");

		_ = await _redisDatabase.KeyExpireAsync(
			redisKey,
			TimeSpan.FromMinutes(1),
			CommandFlags.FireAndForget).ConfigureAwait(false);

		var result = await _rouletteTaskQueueManager.RequestAsync(request).ConfigureAwait(false);

		var response = new RouletteTaskResponse
		{
			Id = result.Id,
			ItemIndex = result.ItemIndex,
			RemainingCount = result.RemainingCount
		};

		return response.ToByteArray();
	}
}
