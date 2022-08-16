namespace Tgs.Promotion.DepositAccumulateRouletteServices;

public interface IDepositAccumulateRouletteEventHandler
{
	ValueTask ClearExpiredEventsAsync(Guid promotionId, string playerCode);
}
