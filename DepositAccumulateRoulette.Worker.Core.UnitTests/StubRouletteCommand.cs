namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class StubRouletteCommand : IRouletteCommand
{
	public TaskCompletionSource Promise { get; } = new();

	public bool IsExecuted { get; private set; }

	public ValueTask ExecuteAsync(
		IServiceProvider serviceProvider,
		IRouletteQueueStore store,
		CancellationToken cancellationToken = default)
	{
		IsExecuted = true;

		Promise.SetResult();

		return ValueTask.CompletedTask;
	}
}
