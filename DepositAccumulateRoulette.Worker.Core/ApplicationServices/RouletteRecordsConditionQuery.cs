namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

public record RouletteRecordsConditionQuery(
	Guid BrandId,
	IEnumerable<string> PlayerCodes,
	IEnumerable<Guid> PromotionIds,
	DateTimeOffset BeginTime,
	DateTimeOffset EndTime);
