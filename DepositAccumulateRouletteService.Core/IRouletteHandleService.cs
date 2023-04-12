using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRouletteService;

public interface IRouletteHandleService
{
	ValueTask<RouletteResult> RequestAsync(RouletteTaskRequest request, CancellationToken cancellationToken);
}
