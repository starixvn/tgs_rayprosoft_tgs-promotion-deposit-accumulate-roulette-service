namespace Tgs.Promotion.DepositAccumulateRouletteServices;

public interface IStatisticsRepository
{
	ValueTask<decimal> IncrementTotalDistributeAmountAsync(
		Guid promotionId,
		decimal amount,
		CancellationToken cancellationToken = default);
}
