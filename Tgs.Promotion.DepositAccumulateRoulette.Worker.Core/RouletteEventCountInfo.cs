namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal record RouletteEventCountInfo(
	DepositAccumulateRouletteEventInfo EventInfo,
	int AvailableCount);
