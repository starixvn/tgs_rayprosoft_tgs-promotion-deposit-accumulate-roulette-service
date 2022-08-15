using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Core;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;

namespace Tgs.Promotion.DepositAccumulateRouletteService.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RouletteController : ControllerBase
{
	[HttpPost]
	public async Task<RouletteResult?> RouletteAsync(
		[FromServices] IMessageSender messageSender,
		[FromBody] ViewModel model)
	{
		var request = new RouletteRequest
		{
			PromotionId = model.PromotionId,
			PlayerCode = model.PlayerCode
		};

		var responseData = await messageSender.RequestAsync(
				"tgs.promotion.depositaccumulateroulette.roulette.play",
				request.ToByteArray(),
				HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var response = RouletteResponse.Parser.ParseFrom(responseData.Span);

		return new RouletteResult(
			response.Id,
			response.ItemIndex,
			response.RemainingCount);
	}

	[HttpPost("event/cache/clear")]
	public async Task CacheClearAsync(
		[FromServices] IDepositAccumulateRouletteEventHandler eventHandler)
	{
		await eventHandler.ClearExpiredEventsAsync(Guid.NewGuid(), "player1").ConfigureAwait(false);
	}

	[HttpPost("promotions/query")]
	public async IAsyncEnumerable<dynamic> GetRoulettePromotionsAsync(
		[FromServices] IMessageSender messageSender,
		[FromBody] string playerCode)
	{
		var request = new RoulettePromotionsReqest
		{
			PlayerCode = playerCode
		};

		var responseData = await messageSender.RequestAsync(
				"tgs.promotion.depositaccumulateroulette.roulettepromotions.query",
				request.ToByteArray(),
				HttpContext.RequestAborted)
			.ConfigureAwait(false);

		var response = RoulettePromotionsResponse.Parser.ParseFrom(responseData.Span);

		foreach (var promotion in response.Promotions)
		{
			yield return new { promotion.PromotionId, promotion.RouletteCount };
		}
	}
}
