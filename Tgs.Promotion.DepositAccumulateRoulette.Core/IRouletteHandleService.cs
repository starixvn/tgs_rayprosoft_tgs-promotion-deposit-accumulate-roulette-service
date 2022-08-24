using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRouletteServices;

public interface IRouletteHandleService
{
	ValueTask<RouletteResult> RequestAsync(RouletteSessionRequest request, CancellationToken cancellationToken);
}
