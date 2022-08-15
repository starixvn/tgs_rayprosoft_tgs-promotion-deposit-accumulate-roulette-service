namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

public interface IRouletteCountRepository
{
	ValueTask<int?> GetRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date);

	ValueTask<int> DecreaseRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date);

	ValueTask<int> IncreaseRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date);

	ValueTask<bool> SetRouletteCountAsync(Guid promotionId, string playerCode, DateOnly date, int rouletteCount);
}
