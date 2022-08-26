using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.MessageQueue;

public class RouletteTaskProcessor : IMessageProcessor
{
	private readonly RouletteTaskQueueManager _rouletteTaskQueueManager;

	public RouletteTaskProcessor(RouletteTaskQueueManager rouletteTaskQueueManager)
	{
		_rouletteTaskQueueManager = rouletteTaskQueueManager ?? throw new ArgumentNullException(nameof(rouletteTaskQueueManager));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
	{
		var request = RouletteTaskRequest.Parser.ParseFrom(data.Span);

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
