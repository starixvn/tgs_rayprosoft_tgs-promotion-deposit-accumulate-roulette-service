using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal record RouletteCommand(
	TaskCompletionSource<RouletteResult> Promise) : IRouletteCommand
{
	public async ValueTask ExecuteAsync(
		IServiceProvider serviceProvider,
		IRouletteQueueStore store,
		CancellationToken cancellationToken = default)
	{
		try
		{
			if (cancellationToken.IsCancellationRequested)
			{
				Promise.SetCanceled(cancellationToken);
				return;
			}

			var serviceCoordinator = serviceProvider.GetRequiredService<IServiceCoordinator>();

			var useEvent = await store.GetAvailableEventAsync(serviceCoordinator, cancellationToken)
					.ConfigureAwait(false);

			if (useEvent.DistributeCount == 0 || useEvent.AvailableCount <= 0)
			{
				var result = new RouletteResult(null, -1);
				Promise.SetResult(result);
				return;
			}

			var playRouletteResult = await serviceCoordinator.GetAsync<PlayRouletteCommand, PlayRouletteResult>(
					new PlayRouletteCommand(store.BrandId, useEvent, store.RouletteItems),
					cancellationToken)
				.ConfigureAwait(false);

			if (playRouletteResult.Result is null)
			{
				var result = new RouletteResult(null, -1);
				Promise.SetResult(result);
				return;
			}

			await store.UpdateEventUsedCountAsync(playRouletteResult.UsedCount)
				.ConfigureAwait(false);

			await serviceCoordinator.ExecuteAsync(
					new EnqueueDistributeBonusJobCommand(
						playRouletteResult.Result.RecordId,
						playRouletteResult.Result.TotalDistributeAmount),
					cancellationToken)
				.ConfigureAwait(false);

			var rouletteResult = new RouletteResult(playRouletteResult.Result.RouletteId, playRouletteResult.Result.ItemIndex);
			Promise.SetResult(rouletteResult);
		}
		catch (Exception ex)
		{
			var logger = serviceProvider.GetRequiredService<ILogger<RouletteCommand>>();
			logger.LogError(
				new EventId(3000),
				ex,
				"Process Roulette with promotion({promotionId}) and player({playerCode}) occurs error.",
				store.PromotionId, store.PlayerCode);

			Promise.SetException(ex);
		}
	}
}
