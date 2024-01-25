using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Tgs.Promotion.DepositAccumulateRouletteService.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRouletteService.ViewModels;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRouletteService.Controllers;

[Route("[controller]")]
[ApiController]
public class PromotionController : ControllerBase
{
	private static readonly JsonSerializerOptions _SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
		Converters = { new JsonStringEnumConverter() }
	};

	[HttpGet("{promotionId}", Name = "GetDepositAccumulateRoulettePromotion")]
	public async ValueTask<PromotionViewModel?> GetDepositAccumulateRoulettePromotionAsync(
		[FromServices] IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotionInfo?> getService,
		Guid promotionId)
	{
		var promotion = await getService.GetAsync(
			new GetDepositAccumulateRoulettePromotionQuery(promotionId),
			HttpContext.RequestAborted)
			.ConfigureAwait(false);

		return promotion is null
			? null
			: new PromotionViewModel
			{
				Id = promotion.Id,
				EventDuring = new EventDuring(promotion.EventDuring.From, promotion.EventDuring.To),
				PromotionLimitAmount = promotion.PromotionLimitAmount,
				IsAutoSent = promotion.IsAutoSent,
				CampaignDetail = JsonSerializer.SerializeToDocument(promotion.CampaignDetail, _SerializerOptions)
			};
	}
}
