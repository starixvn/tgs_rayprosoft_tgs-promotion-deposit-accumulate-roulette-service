namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal record PlayRouletteResult(
	PlayRouletteResult.RouletteResult? Result,
	int UsedCount)
{
	internal record RouletteResult(
		Guid RecordId,
		decimal TotalAmount,
		decimal Bonus,
		decimal Bucket,
		Guid RouletteId,
		int ItemIndex);
}
