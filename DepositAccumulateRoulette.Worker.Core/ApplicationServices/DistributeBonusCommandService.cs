using Microsoft.Extensions.Logging;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class DistributeBonusCommandService : ICommandService<DistributeBonusCommand>
{
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo> _getDistributePlanService;
	private readonly IDistributePlanHasher _hasher;
	private readonly ILogger<DistributeBonusCommandService> _logger;

	public DistributeBonusCommandService(
		IPromotionServiceClient promotionServiceClient,
		IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo> getDistributePlanService,
		IDistributePlanHasher hasher,
		ILogger<DistributeBonusCommandService> logger)
	{
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_getDistributePlanService = getDistributePlanService ?? throw new ArgumentNullException(nameof(getDistributePlanService));
		_hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask ExecuteAsync(
		DistributeBonusCommand command,
		CancellationToken cancellationToken = default)
	{
		var hash = _hasher.GetHashCode(command.EventDate, command.CurrentRouletteCount);

		_logger.LogInformation(
			"Hash generate with Date: {date}, Count: {count}; Hash: {hash}",
			command.EventDate, command.CurrentRouletteCount, hash);

		var plan = await _getDistributePlanService.GetAsync(
			new GetPromotionDistributePlanQuery(
				command.PromotionId,
				hash), cancellationToken);

		_ = await _promotionServiceClient.DistributePromotionBonusAsync(
			plan.Id,
			command.PlayerCode,
			command.Bonus,
			command.Bucket,
			null,
			cancellationToken).ConfigureAwait(false);
	}
}
