namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal interface IRouletteCommand
{
	ValueTask ExecuteAsync(
		IServiceProvider serviceProvider,
		IRouletteQueueStore store,
		CancellationToken cancellationToken = default);
}
