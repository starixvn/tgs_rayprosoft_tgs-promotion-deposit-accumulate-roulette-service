namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

public interface IClearDepositAccumulateRouletteEventCache
{
	ValueTask ClearCacheAsync(Guid promotionId, string playerCode);
}
