namespace Tgs.Promotion.DepositAccumulateRouletteService;

public class Paging
{
	public bool? IsDesc { get; set; }

	public int? Page { get; set; }

	public int? Rows { get; set; }

	public bool? ShowAll { get; set; } = false;

	public string? SortBy { get; set; }
}
