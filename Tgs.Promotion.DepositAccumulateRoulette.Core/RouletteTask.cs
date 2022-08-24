namespace Tgs.Promotion.DepositAccumulateRouletteServices;

internal record RouletteTask(
	RouletteTaskRequest Request,
	TaskCompletionSource<RouletteResult> Promise);
