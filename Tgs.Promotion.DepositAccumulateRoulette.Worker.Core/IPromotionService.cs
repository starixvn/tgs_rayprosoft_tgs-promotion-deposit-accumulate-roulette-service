namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal interface IPromotionService
{
	IAsyncEnumerable<Guid> GetPlayerAvailablePromotionIdsAsync(
		string playerCode,
		CancellationToken cancellationToken = default);
}
