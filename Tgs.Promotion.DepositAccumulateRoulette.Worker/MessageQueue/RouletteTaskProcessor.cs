using Google.Protobuf;
using StackExchange.Redis;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.MessageQueue;

public class RouletteTaskProcessor : IMessageProcessor
{
	private readonly IGetService<RouletteTaskCommand, RouletteResult> _rouletteService;
	private readonly IDatabase _redisDatabase;

	public RouletteTaskProcessor(
		IGetService<RouletteTaskCommand, RouletteResult> rouletteService,
		IRedisConnectionManager redisConnectionManager)
	{
		_rouletteService = rouletteService ?? throw new ArgumentNullException(nameof(rouletteService));

		if (redisConnectionManager is null)
			throw new ArgumentNullException(nameof(redisConnectionManager));

		_redisDatabase = redisConnectionManager.Database;
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
	{
		var request = RouletteTaskRequest.Parser.ParseFrom(data.Span);

		var redisKey = $"Promotion:DepositAccumulateRoulette:Worker:{request.PlayerCode}:{request.PromotionId.ToGuid()}";

		var subject = await _redisDatabase.StringGetAsync(redisKey).ConfigureAwait(false);

		if (!subject.HasValue || (string)subject! != Environment.MachineName)
			throw new InvalidOperationException($"This machine ({Environment.MachineName}) not match {subject}");

		_ = await _redisDatabase.KeyExpireAsync(
			redisKey,
			TimeSpan.FromMinutes(1),
			CommandFlags.FireAndForget).ConfigureAwait(false);

		var command = new RouletteTaskCommand(request.PromotionId, request.PlayerCode);

		var result = await _rouletteService.GetAsync(command, cancellationToken).ConfigureAwait(false);

		var response = new RouletteTaskResponse
		{
			Id = result.Id,
			ItemIndex = result.ItemIndex
		};

		return response.ToByteArray();
	}
}
