using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal record RouletteTask(
	RouletteTaskRequest Request,
	TaskCompletionSource<RouletteResult> Promise);
