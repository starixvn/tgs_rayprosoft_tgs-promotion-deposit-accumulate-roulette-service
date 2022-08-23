namespace Tgs.Promotion.DepositAccumulateRoulette.Background.ApplicationServices;

internal record ClearExpiredEventCacheCommand(
	IEnumerable<ClearCacheInfo> Infos);
