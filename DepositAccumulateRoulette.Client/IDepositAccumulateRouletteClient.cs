namespace Tgs.DepositAccumulateRoulette.Clients;

public interface IDepositAccumulateRouletteClient
{
	ValueTask<RouletteResult> RouletteAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default);
}
