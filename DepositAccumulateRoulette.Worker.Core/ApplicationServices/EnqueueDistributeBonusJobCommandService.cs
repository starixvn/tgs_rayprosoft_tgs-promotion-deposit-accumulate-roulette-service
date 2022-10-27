using Hangfire;
using Microsoft.Extensions.Logging;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class EnqueueDistributeBonusJobCommandService : ICommandService<EnqueueDistributeBonusJobCommand>
{
	private readonly IDistributePlanHasher _hasher;
	private readonly IBackgroundJobClient _backgroundJobClient;
	private readonly ILogger<EnqueueDistributeBonusJobCommandService> _logger;

	public EnqueueDistributeBonusJobCommandService(
		IDistributePlanHasher hasher,
		IBackgroundJobClient backgroundJobClient,
		ILogger<EnqueueDistributeBonusJobCommandService> logger)
	{
		_hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
		_backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public ValueTask ExecuteAsync(
		EnqueueDistributeBonusJobCommand command,
		CancellationToken cancellationToken = default)
	{
		var hash = _hasher.GetHashCode(command.EventDate, command.UsedCount);

		_logger.LogInformation(
			"Hash generate with Date: {date}, Count: {count}; Hash: {hash}",
			command.EventDate, command.UsedCount, hash);

		_ = _backgroundJobClient.Enqueue<DistributeBonusBackgroundJob>(job
			=> job.ExecuteAsync(
				new DistributeBonusScheduleInfo(
					command.PromotionId,
					command.PlayerCode,
					command.EventDate,
					command.UsedCount,
					command.PlayResult,
					hash)));

		return ValueTask.CompletedTask;
	}
}
