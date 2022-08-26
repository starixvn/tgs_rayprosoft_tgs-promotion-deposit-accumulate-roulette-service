using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

public class RouletteProcessor : IMessageProcessor
{
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IRouletteHandleService _handleService;

	public RouletteProcessor(
		IPromotionServiceClient promotionServiceClient,
		IPlayerServiceClient playerServiceClient,
		IRouletteHandleService handleService)
	{
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_handleService = handleService ?? throw new ArgumentNullException(nameof(handleService));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = RouletteRequest.Parser.ParseFrom(data.Span);

		var player = await _playerServiceClient.GetPlayerInfoAsync(request.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		if (player is null)
			throw new InvalidOperationException($"Can't find player with code {request.PlayerCode}");

		var mainInfo = await _promotionServiceClient
			.GetPromotionMainInfoAsync(request.PromotionId, cancellationToken)
			.ConfigureAwait(false);

		if (mainInfo is null)
			throw new InvalidOperationException($"Promotion with id {request.PromotionId} does not exist");

		if (mainInfo.PromotionType != "DepositAccumulateRoulette")
			throw new InvalidOperationException($"Promotion with id {request.PromotionId} is not a DepositAccumulateRoulette promotion");

		var rule = await _promotionServiceClient
			.GetPromotionRuleAsync<PromotionRule>(request.PromotionId, cancellationToken)
			.ConfigureAwait(false);

		if (rule is null)
			throw new InvalidOperationException("DepositAccumulateRoulette setting is null");

		if (!rule.CompaignDetail.RouletteItems.Any())
			throw new InvalidOperationException($"Promotion {request.PromotionId} does not have any roulette items.");

		var result = await _handleService.RequestAsync(request, cancellationToken).ConfigureAwait(false);

		return new RouletteResponse
		{
			Id = result.Id,
			ItemIndex = result.ItemIndex,
			RemainingCount = result.RemainingCount
		}.ToByteArray();
	}

	internal class PromotionRule
	{
		public DepositAccumulateRouletteSettings CompaignDetail { get; init; } = default!;
	}
}
