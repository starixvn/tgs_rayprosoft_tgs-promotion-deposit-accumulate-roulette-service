using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;
using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

internal class DepositAccumulateRouletteSettingProcessor : IMessageProcessor
{
    private readonly DepositAccumulateRouletteSerializer _serializer;

    public DepositAccumulateRouletteSettingProcessor(
        DepositAccumulateRouletteSerializer serializer)
    {
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    public ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = CampaignDetailRequest.Parser.ParseFrom(data.Span);

        var settings = _serializer.Deserialize<DepositAccumulateRouletteSettings>(request.Settings);

        if (settings is null)
            throw new InvalidOperationException("Settings is null");

        if (!settings.DepositItems.Any() || !settings.RouletteItems.Any())
            throw new InvalidOperationException("Missing DepositItems or RouletteItems");

        var totalProbability = settings.RouletteItems.Sum(x => x.WinningProbability);

        if (totalProbability != 1M)
            throw new InvalidOperationException("Summary winning probability should be 1");

        var response = new CampaignDetailResponse
        {
            CampaignDetail = _serializer.Serialize(settings)
        };

        return ValueTask.FromResult((ReadOnlyMemory<byte>)response.ToByteArray());
    }
}