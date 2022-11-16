namespace Tgs.Promotion.DepositAccumulateRouletteService.ViewModels;

public class RouletteRecordView
{
	public DateTimeOffset CreatedOn { get; init; }

	public DateOnly EventDate { get; init; }

	public Guid PromotionId { get; init; }

	public Guid RouletteId { get; init; }

	public int ItemIndex { get; init; }

	public string PlayerCode { get; init; } = default!;
}
