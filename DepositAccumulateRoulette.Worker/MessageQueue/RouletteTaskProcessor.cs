using Google.Protobuf;
using StackExchange.Redis;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.MessageQueue;

public class RouletteTaskProcessor : IMessageProcessor<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>
{
	private readonly IGetService<RouletteTaskCommand, RouletteResult> _rouletteService;
	private readonly IDatabase _redisDatabase;

	public RouletteTaskProcessor(
		IGetService<RouletteTaskCommand, RouletteResult> rouletteService,
		IRedisConnectionManager redisConnectionManager)
	{
		_rouletteService = rouletteService ?? throw new ArgumentNullException(nameof(rouletteService));

		ArgumentNullException.ThrowIfNull(redisConnectionManager);

		_redisDatabase = redisConnectionManager.Database;
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = RouletteTaskRequest.Parser.ParseFrom(data.Span);

		var redisKey = $"Promotion:DepositAccumulateRoulette:Worker:{request.PlayerCode}:{request.PromotionId.ToGuid()}";

		var resource = await _redisDatabase.StringGetAsync(redisKey).ConfigureAwait(false);

		if (!resource.HasValue || (string)resource! != Environment.MachineName)
			throw new InvalidOperationException($"This machine ({Environment.MachineName}) not match {resource}");

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
