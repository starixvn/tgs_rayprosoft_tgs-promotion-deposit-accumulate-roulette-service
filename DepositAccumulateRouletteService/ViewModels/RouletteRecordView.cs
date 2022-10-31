namespace Tgs.Promotion.DepositAccumulateRouletteService.ViewModels;

public class RouletteRecordView
{
	public Guid RouletteId { get; init; }

	public DateOnly EventDate { get; init; }

	public int ItemIndex { get; init; }

	public DateTimeOffset CreatedOn { get; init; }
}
