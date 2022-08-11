using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

internal record RouletteCountInfo(
	DepositAccumulateRouletteEventInfo? UseEvent,
	int BeforeEventCount,
	int AfterEventCount,
	int BeforeTotalCount,
	int AfterTotalCount);
