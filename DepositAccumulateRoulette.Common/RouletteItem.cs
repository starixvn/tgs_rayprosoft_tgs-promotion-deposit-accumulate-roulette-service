namespace Tgs.Promotion.DepositAccumulateRoulette.Common;

public record RouletteItem(
	Guid Id,
	string Title,
	decimal TurnoverRate,
	decimal WinningProbability,
	decimal Bonus);
