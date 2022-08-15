namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

public interface IDepositAccumulateRouletteEventHandler
{
	ValueTask ClearExpiredEventsAsync(Guid promotionId, string playerCode);
}
