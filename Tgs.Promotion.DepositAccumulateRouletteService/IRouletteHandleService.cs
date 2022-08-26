using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;

namespace Tgs.Promotion.DepositAccumulateRouletteService;

public interface IRouletteHandleService
{
	ValueTask<RouletteResult> RequestAsync(RouletteRequest request, CancellationToken cancellationToken);
}
