using Hangfire;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class EnqueueDistributeBonusJobCommandService(
	IBackgroundJobClient backgroundJobClient) : ICommandService<EnqueueDistributeBonusJobCommand>
{
	private readonly IBackgroundJobClient _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));

	public ValueTask ExecuteAsync(
		EnqueueDistributeBonusJobCommand command,
		CancellationToken cancellationToken = default)
	{
		_ = _backgroundJobClient.Enqueue<DistributeBonusBackgroundJob>(job
			=> job.ExecuteAsync(
				new DistributeBonusScheduleInfo(
					command.RouletteRecordId,
					command.TotalDistributeAmount)));

		return ValueTask.CompletedTask;
	}
}
