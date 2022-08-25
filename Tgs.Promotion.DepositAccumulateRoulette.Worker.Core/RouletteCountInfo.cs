namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal record RouletteCountInfo(
	DepositAccumulateRouletteEventInfo? UseEvent,
	int BeforeEventCount,
	int AfterEventCount,
	int BeforeTotalCount,
	int AfterTotalCount);
