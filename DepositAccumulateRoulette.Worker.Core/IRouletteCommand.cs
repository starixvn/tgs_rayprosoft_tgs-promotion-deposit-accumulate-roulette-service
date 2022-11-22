using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal interface IRouletteCommand
{
	TaskCompletionSource<RouletteResult> Promise { get; }

	ValueTask ExecuteAsync(
		IServiceProvider serviceProvider,
		IRouletteQueueStore store,
		CancellationToken cancellationToken = default);
}
