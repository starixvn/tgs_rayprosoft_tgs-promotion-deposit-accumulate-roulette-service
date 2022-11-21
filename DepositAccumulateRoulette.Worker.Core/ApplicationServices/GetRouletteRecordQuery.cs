namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

public record GetRouletteRecordQuery(
	Guid BrandId,
	Guid RecordId);
