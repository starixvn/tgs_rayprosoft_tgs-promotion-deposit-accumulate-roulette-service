using System.Text.Json;

namespace Tgs.Promotion.DepositAccumulateRouletteService.ViewModels;

public class PromotionViewModel
{
	public Guid Id { get; init; }

	public EventDuring EventDuring { get; init; } = default!;

	public decimal? PromotionLimitAmount { get; init; }

	public bool IsAutoSent { get; init; }

	public JsonDocument CampaignDetail { get; init; } = default!;
}
