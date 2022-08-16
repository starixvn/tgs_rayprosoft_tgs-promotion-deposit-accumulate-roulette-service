using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;
using Tgs.Promotion.DepositAccumulateRouletteServices;
using Tgs.Promotion.DepositAccumulateRouletteServices.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

internal class RouletteSettingQueryProcessor : IMessageProcessor
{
	private readonly IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?> _getPromotionService;

	public RouletteSettingQueryProcessor(
		IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?> getPromotionService)
	{
		_getPromotionService = getPromotionService ?? throw new ArgumentNullException(nameof(getPromotionService));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		ReadOnlyMemory<byte> data,
		CancellationToken cancellationToken = default)
	{
		var request = RouletteSettingQueryRequest.Parser.ParseFrom(data.Span);

		var promotion = await _getPromotionService
			.GetAsync(
				new GetDepositAccumulateRoulettePromotionQuery(request.PromotionId),
				cancellationToken)
			.ConfigureAwait(false);

		if (promotion == null)
			return null;

		var items = promotion.CampaignDetail.RouletteItems
			.Select(item => new RouletteItem
			{
				Id = item.Id,
				Title = item.Title
			});

		var response = new RouletteSettingQueryResponse
		{
			PromotionId = promotion.Id
		};

		response.Items.AddRange(items);

		return response.ToByteArray();
	}
}
