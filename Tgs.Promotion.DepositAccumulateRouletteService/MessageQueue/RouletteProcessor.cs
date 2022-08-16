using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;
using Tgs.Promotion.DepositAccumulateRouletteServices;
using Tgs.Promotion.DepositAccumulateRouletteServices.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

public class RouletteProcessor : IMessageProcessor
{
	private readonly IGetService<RouletteCommand, RouletteResult> _commandService;

	public RouletteProcessor(
		IGetService<RouletteCommand, RouletteResult> commandService)
	{
		_commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = RouletteRequest.Parser.ParseFrom(data.Span);

		var command = new RouletteCommand(request.PromotionId, request.PlayerCode);

		var result = await _commandService.GetAsync(command, cancellationToken).ConfigureAwait(false);

		return new RouletteResponse
		{
			Id = result.Id,
			ItemIndex = result.ItemIndex,
			RemainingCount = result.RemainingCount
		}.ToByteArray();
	}
}
