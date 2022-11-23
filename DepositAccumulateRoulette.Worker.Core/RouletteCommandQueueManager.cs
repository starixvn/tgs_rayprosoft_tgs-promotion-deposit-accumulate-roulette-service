using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class RouletteCommandQueueManager
{
	private readonly IServiceProvider _serviceProvider;

	private readonly ConcurrentDictionary<string, RouletteCommandQueue> _queuePool = new();
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	public RouletteCommandQueueManager(
		IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}

	public async Task<RouletteResult> RequestAsync(RouletteCommandRequest request)
	{
		var key = $"{request.PlayerCode}:{request.PromotionId}";

		var queue = _queuePool.GetOrAdd(
			key,
			_ => ActivatorUtilities.CreateInstance<RouletteCommandQueue>(
				_serviceProvider,
				request.PlayerCode,
				request.PromotionId));

		if (!queue.IsStartProcess)
			await queue.StartProcessAsync(_cancellationTokenSource.Token)
				.ConfigureAwait(false);

		var command = new RouletteCommand(new TaskCompletionSource<RouletteResult>());
		queue.Enqueue(command);

		return await command.Promise.Task.ConfigureAwait(false);
	}

	public void StopReceive()
	{
		if (!_cancellationTokenSource.IsCancellationRequested)
			_cancellationTokenSource.Cancel();

		Task.WaitAll(_queuePool.Values
			.Select(queue => Task.Run(queue.StopProcess)).ToArray());
	}

	public void ClearExpiredQueues()
	{
		var currentTime = DateTime.Now;

		var removeTasks = _queuePool.Values
			.Where(queue => currentTime > queue.Expired)
			.Select(queue => Task.Run(() =>
			{
				var key = $"{queue.PlayerCode}:{queue.PromotionId}";

				if (!_queuePool.TryRemove(key, out _))
					return;

				queue.StopProcess();
				queue.Dispose();
			})).ToArray();

		Task.WaitAll(removeTasks);
	}
}
