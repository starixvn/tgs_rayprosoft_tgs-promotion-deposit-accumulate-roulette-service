namespace Tgs.Promotion.DepositAccumulateRoulette.Background.ApplicationServices;

public record ClearExpiredEventCacheCommand(
	IEnumerable<ClearCacheInfo> Infos);
