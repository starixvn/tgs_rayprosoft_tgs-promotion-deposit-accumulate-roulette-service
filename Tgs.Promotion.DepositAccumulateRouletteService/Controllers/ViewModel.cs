namespace Tgs.Promotion.DepositAccumulateRouletteService.Controllers;

public class ViewModel
{
	public Guid PromotionId { get; set; }

	public string PlayerCode { get; set; } = default!;

	public int CurrentRouletteCount { get; set; }

	public Guid EventId { get; set; }
}
