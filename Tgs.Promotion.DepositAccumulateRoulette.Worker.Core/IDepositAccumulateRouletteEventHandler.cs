namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public interface IDepositAccumulateRouletteEventHandler
{
	ValueTask ClearExpiredEventsAsync(Guid promotionId, string playerCode);
}
