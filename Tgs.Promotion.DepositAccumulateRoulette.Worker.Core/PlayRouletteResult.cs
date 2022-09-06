namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal record PlayRouletteResult(
	Guid RecordId,
	decimal TotalAmount,
	decimal Bonus,
	decimal Bucket,
	Guid RouletteId,
	int ItemIndex);
