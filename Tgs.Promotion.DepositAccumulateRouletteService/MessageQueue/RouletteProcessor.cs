using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;
using Tgs.Promotion.DepositAccumulateRouletteServices;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

public class RouletteProcessor : IMessageProcessor
{
	private readonly RouletteTaskProcessor _rouletteTaskProcessor;

	public RouletteProcessor(
		RouletteTaskProcessor rouletteTaskProcessor)
	{
		_rouletteTaskProcessor = rouletteTaskProcessor ?? throw new ArgumentNullException(nameof(rouletteTaskProcessor));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = RouletteRequest.Parser.ParseFrom(data.Span);

		var taskRequest = new RouletteTaskRequest(request.PromotionId, request.PlayerCode);

		var result = await _rouletteTaskProcessor.RequestAsync(taskRequest).ConfigureAwait(false);

		return new RouletteResponse
		{
			Id = result.Id,
			ItemIndex = result.ItemIndex,
			RemainingCount = result.RemainingCount
		}.ToByteArray();
	}
}
