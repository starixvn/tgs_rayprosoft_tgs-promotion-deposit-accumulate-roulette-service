namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ViewModels;

public class RouletteRecordViewModel
{
	public Guid RouletteId { get; init; }

	public DateOnly EventDate { get; init; }

	public int ItemIndex { get; init; }

	public DateTimeOffset CreatedOn { get; init; }
}
