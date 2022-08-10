namespace Tgs.Promotion.DepositAccumulateRoulette.Client;

public interface IDepositAccumulateRouletteClient
{
    public ValueTask<RouletteSetting?> GetRouletteSettingAsync(
        Guid promotionId,
        CancellationToken cancellationToken = default);
}