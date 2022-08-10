using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Background.Core.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRoulette.Background.Protos;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.MessageQueue;

internal class DepositAccumulateCalculateHandler : IMessageHandler
{
    private readonly ICommandService<DepositAccumulatedCalculateCommand> _commandService;

    public DepositAccumulateCalculateHandler(
        ICommandService<DepositAccumulatedCalculateCommand> commandService)
    {
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
    }

    public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = ProcessRequest.Parser.ParseFrom(data.Span);

        var requestDate = request.ProcessDate.ToDateOnly();

        if (requestDate is null)
            return ValueTask.CompletedTask;

        var command = new DepositAccumulatedCalculateCommand(requestDate.Value);

        return _commandService.ExecuteAsync(command, cancellationToken);
    }
}