using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;
using Tgs.Promotion.DepositAccumulateRouletteServices;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

public class RouletteProcessor : IMessageProcessor
{
	private readonly IRouletteHandleService _handleService;

	public RouletteProcessor(
		IRouletteHandleService handleService)
	{
		_handleService = handleService ?? throw new ArgumentNullException(nameof(handleService));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = RouletteRequest.Parser.ParseFrom(data.Span);

		var sessionRequest = new RouletteSessionRequest
		{
			PromotionId = request.PromotionId,
			PlayerCode = request.PlayerCode
		};

		var result = await _handleService.RequestAsync(sessionRequest, cancellationToken).ConfigureAwait(false);

		return new RouletteResponse
		{
			Id = result.Id,
			ItemIndex = result.ItemIndex,
			RemainingCount = result.RemainingCount
		}.ToByteArray();
	}
}
