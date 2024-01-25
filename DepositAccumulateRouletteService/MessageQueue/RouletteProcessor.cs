using Google.Protobuf;
using Tgs.PlayerService.Client;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Tgs.Promotion.DepositAccumulateRouletteService.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

public class RouletteProcessor(
	IPlayerServiceClient playerServiceClient,
	IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotionInfo?> getPromotionService,
	IRouletteHandleService handleService) : IMessageProcessor<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>
{
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
	private readonly IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotionInfo?> _getPromotionService = getPromotionService ?? throw new ArgumentNullException(nameof(getPromotionService));
	private readonly IRouletteHandleService _handleService = handleService ?? throw new ArgumentNullException(nameof(handleService));

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		var request = RouletteRequest.Parser.ParseFrom(data.Span);

		_ = await _playerServiceClient.GetPlayerInfoAsync(request.PlayerCode, cancellationToken)
			.ConfigureAwait(false)
			?? throw new InvalidOperationException($"Can't find player with code {request.PlayerCode}");

		var promotion = await _getPromotionService
			.GetAsync(
				new GetDepositAccumulateRoulettePromotionQuery(request.PromotionId),
				cancellationToken)
			.ConfigureAwait(false)
			?? throw new InvalidOperationException($"Promotion with id {request.PromotionId} does not exist");

		if (!promotion.CampaignDetail.RouletteItems.Any())
			throw new InvalidOperationException($"Promotion {request.PromotionId} does not have any roulette items.");

		var taskRequest = new RouletteTaskRequest
		{
			PromotionId = request.PromotionId,
			PlayerCode = request.PlayerCode
		};

		var result = await _handleService.RequestAsync(taskRequest, cancellationToken).ConfigureAwait(false);

		return new RouletteResponse
		{
			Id = result.Id,
			ItemIndex = result.ItemIndex
		}.ToByteArray();
	}

	internal record PromotionRule(DepositAccumulateRouletteSettings CampaignDetail);
}
