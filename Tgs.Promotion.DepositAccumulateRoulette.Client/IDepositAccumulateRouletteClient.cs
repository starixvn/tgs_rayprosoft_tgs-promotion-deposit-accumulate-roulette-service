namespace Tgs.Promotion.DepositAccumulateRoulette.Client;

public interface IDepositAccumulateRouletteClient
{
	ValueTask<RouletteSetting?> GetRouletteSettingAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default);

	ValueTask<RouletteResult> RouletteAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default);
}
