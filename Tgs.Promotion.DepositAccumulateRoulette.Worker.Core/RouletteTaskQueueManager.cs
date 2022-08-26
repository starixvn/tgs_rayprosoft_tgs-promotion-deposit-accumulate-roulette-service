using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public class RouletteTaskQueueManager
{
	private readonly IServiceProvider _serviceProvider;

	private readonly ConcurrentDictionary<string, RouletteTaskQueue> _queuePool = new();
	private readonly CancellationTokenSource _cancellationTokenSource = new();

	public RouletteTaskQueueManager(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}

	public Task<RouletteResult> RequestAsync(RouletteTaskRequest request)
	{
		var queue = _queuePool.GetOrAdd(
			request.PlayerCode,
			_ => new RouletteTaskQueue(
				request.PlayerCode,
				_serviceProvider,
				_serviceProvider.GetRequiredService<ILogger<RouletteTaskQueue>>()));

		if (!queue.IsStartProcess)
			queue.StartProcess(_cancellationTokenSource.Token);

		var task = new RouletteTask(request, new TaskCompletionSource<RouletteResult>());
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
}
