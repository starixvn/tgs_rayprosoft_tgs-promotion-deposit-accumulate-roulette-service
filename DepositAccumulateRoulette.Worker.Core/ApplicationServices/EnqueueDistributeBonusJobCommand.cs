namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal record EnqueueDistributeBonusJobCommand(
	Guid RouletteRecordId,
	decimal TotalDistributeAmount);
