namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal record DistributeBonusScheduleInfo(
	Guid RouletteRecordId,
	decimal TotalDistributeAmount);
