using Microsoft.AspNetCore.Mvc;

namespace Tgs.Promotion.DepositAccumulateRouletteService.ViewModels;

public class RouletteRecordsQueryViewModel
{
	[FromQuery(Name = "playerCode")]
	public string[] PlayerCodes { get; init; } = [];

	[FromQuery(Name = "promotionId")]
	public Guid[] PromotionIds { get; init; } = [];

	[FromQuery(Name = "begin")]
	public DateTimeOffset Begin { get; init; }

	[FromQuery(Name = "end")]
	public DateTimeOffset End { get; init; }
}
