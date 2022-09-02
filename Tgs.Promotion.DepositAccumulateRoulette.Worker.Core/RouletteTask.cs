using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal record RouletteTask(
	RouletteTaskQueueRequest Request,
	TaskCompletionSource<RouletteResult> Promise);
