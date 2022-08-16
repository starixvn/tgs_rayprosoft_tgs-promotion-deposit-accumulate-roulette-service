namespace Tgs.Promotion.DepositAccumulateRouletteServices;

internal record RouletteCountInfo(
	DepositAccumulateRouletteEventInfo? UseEvent,
	int BeforeEventCount,
	int AfterEventCount,
	int BeforeTotalCount,
	int AfterTotalCount);
