using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Background.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRoulette.Background.Protos;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.MessageQueue;

internal class DistributeDepositAccumulateRouletteCountHandler : IMessageHandler
{
	private readonly ICommandService<DistributeDepositAccumulateRouletteCountCommand> _commandService;

	public DistributeDepositAccumulateRouletteCountHandler(
		ICommandService<DistributeDepositAccumulateRouletteCountCommand> commandService)
	{
		_commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = ProcessRequest.Parser.ParseFrom(data.Span);

		var requestDate = request.ProcessDate.ToDateOnly();

		if (requestDate is null)
			return ValueTask.CompletedTask;

		var command = new DistributeDepositAccumulateRouletteCountCommand(requestDate.Value);

		return _commandService.ExecuteAsync(command, cancellationToken);
	}
}
