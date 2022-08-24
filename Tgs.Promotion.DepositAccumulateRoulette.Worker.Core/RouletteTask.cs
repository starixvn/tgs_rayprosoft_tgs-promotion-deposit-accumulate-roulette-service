using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal record RouletteTask(
	RouletteSessionRequest Request,
	TaskCompletionSource<RouletteResult> Promise);
