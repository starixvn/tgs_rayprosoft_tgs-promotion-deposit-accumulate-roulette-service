namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ViewModels;

public class RouletteRecordViewModel
{
	public DateTimeOffset CreatedOn { get; init; }

	public DateOnly EventDate { get; init; }

	public int ItemIndex { get; init; }

	public string PlayerCode { get; init; } = default!;

	public Guid PromotionId { get; init; }

	public Guid RouletteId { get; init; }
}
