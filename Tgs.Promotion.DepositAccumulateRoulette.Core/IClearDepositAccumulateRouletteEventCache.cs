namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

internal interface IClearDepositAccumulateRouletteEventCache
{
	ValueTask ClearCacheAsync(Guid promotionId, string playerCode);
}
