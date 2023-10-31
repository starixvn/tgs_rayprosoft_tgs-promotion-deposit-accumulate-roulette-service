using Google.Protobuf;
using Tgs.PlayerService.Client;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Tgs.Promotion.DepositAccumulateRouletteService.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

public class RouletteProcessor : IMessageProcessor
{
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotionInfo?> _getPromotionService;
	private readonly IRouletteHandleService _handleService;

	public RouletteProcessor(
		IPlayerServiceClient playerServiceClient,
		IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotionInfo?> getPromotionService,
		IRouletteHandleService handleService)
	{
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_getPromotionService = getPromotionService ?? throw new ArgumentNullException(nameof(getPromotionService));
		_handleService = handleService ?? throw new ArgumentNullException(nameof(handleService));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
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
