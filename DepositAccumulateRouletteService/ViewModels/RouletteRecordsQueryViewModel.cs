using Microsoft.AspNetCore.Mvc;

namespace Tgs.Promotion.DepositAccumulateRouletteService.ViewModels;

public class RouletteRecordsQueryViewModel
{
	[FromQuery(Name = "playerCode")]
	public string[] PlayerCodes { get; init; } = Array.Empty<string>();

	[FromQuery(Name = "promotionId")]
	public Guid[] PromotionIds { get; init; } = Array.Empty<Guid>();

	[FromQuery(Name = "begin")]
	public DateTimeOffset Begin { get; init; }

	[FromQuery(Name = "end")]
	public DateTimeOffset End { get; init; }
}
