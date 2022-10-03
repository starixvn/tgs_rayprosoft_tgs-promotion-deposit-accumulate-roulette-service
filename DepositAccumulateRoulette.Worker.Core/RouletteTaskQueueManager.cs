using System.Collections.Concurrent;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class RouletteTaskQueueManager
{
	private readonly IServiceProvider _serviceProvider;
	private readonly IBackgroundJobClient _backgroundJobClient;
	private readonly ISystemClock _systemClock;

	private readonly ConcurrentDictionary<string, RouletteTaskQueue> _queuePool = new();
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	public RouletteTaskQueueManager(
		IServiceProvider serviceProvider,
		IBackgroundJobClient backgroundJobClient,
		ISystemClock systemClock)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_backgroundJobClient = backgroundJobClient;
		_systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
	}

	public Task<RouletteResult> RequestAsync(RouletteTaskQueueRequest request)
	{
		var key = $"{request.PlayerCode}:{request.PromotionId}";

		var queue = _queuePool.GetOrAdd(
			key,
			_ => new RouletteTaskQueue(
				request.PlayerCode,
				request.PromotionId,
				_serviceProvider,
				_backgroundJobClient,
				_systemClock,
				_serviceProvider.GetRequiredService<ILogger<RouletteTaskQueue>>()));

		if (!queue.IsStartProcess)
			queue.StartProcess(_cancellationTokenSource.Token);

		var task = new RouletteTask(new TaskCompletionSource<RouletteResult>());
		queue.Enqueue(task);

		return task.Promise.Task;
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
