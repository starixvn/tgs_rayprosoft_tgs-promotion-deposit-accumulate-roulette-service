using Tgs.PlayerService.Client;
using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal record PlayRouletteCommand(
	PlayerInfo PlayerInfo,
	RouletteEventCountInfo EventInfo,
	IEnumerable<RouletteItem> RouletteItems);
