using Microsoft.Extensions.Logging;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class ManuelDistributeBonusCommandService(
	ITgsRepository tgsRepository,
	IDistributePlanHasher hasher,
	ICommandService<DistributeBonusCommand> distributeService,
	ILogger<ManuelDistributeBonusCommandService> logger) : ICommandService<ManuelDistributeBonusCommand>
{
	private readonly ITgsRepository _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
	private readonly IDistributePlanHasher _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
	private readonly ICommandService<DistributeBonusCommand> _distributeService = distributeService ?? throw new ArgumentNullException(nameof(distributeService));
	private readonly ILogger<ManuelDistributeBonusCommandService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	public async ValueTask ExecuteAsync(
		ManuelDistributeBonusCommand command,
		CancellationToken cancellationToken = default)
	{
		var rouletteRecordInfo = await _tgsRepository
			.GetRouletteRecordInfoAsync(
				command.RouletteRecordId,
				cancellationToken)
			.ConfigureAwait(false);

		if (rouletteRecordInfo == null)
		{
			_logger.LogInformation("Cannot find roulette record with id '{rouletteRecordId}' to process manual distribute", command.RouletteRecordId);
			return;
		}

		var distributePlanHash = _hasher.GetHashCode(rouletteRecordInfo.EventDate, rouletteRecordInfo.RouletteCount);

		_logger.LogInformation(
			"Hash generate with Date: {date}, Count: {count}; Hash: {hash}",
			rouletteRecordInfo.EventDate, rouletteRecordInfo.RecordId, distributePlanHash);

		await _distributeService
			.ExecuteAsync(
				new DistributeBonusCommand(
					rouletteRecordInfo.PromotionId,
					rouletteRecordInfo.PlayerCode,
					distributePlanHash,
					rouletteRecordInfo.BonusAmount,
					rouletteRecordInfo.BucketAmount,
					rouletteRecordInfo.RecordId),
				cancellationToken).ConfigureAwait(false);
	}
}
