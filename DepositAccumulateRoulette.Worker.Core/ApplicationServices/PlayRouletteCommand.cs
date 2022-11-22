using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal record PlayRouletteCommand(
	Guid BrandId,
	RouletteEventCountInfo EventInfo,
	IEnumerable<RouletteItem> RouletteItems);
