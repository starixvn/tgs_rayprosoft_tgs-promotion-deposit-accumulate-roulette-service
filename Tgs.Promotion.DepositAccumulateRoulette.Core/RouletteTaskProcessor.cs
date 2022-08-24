using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tgs.Promotion.DepositAccumulateRouletteServices;

public class RouletteTaskProcessor
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<RouletteTaskProcessor> _logger;

	private readonly ConcurrentDictionary<string, RouletteTaskQueue> _queuePool = new();

	private readonly CancellationTokenSource _cancellationTokenSource = new();

	public RouletteTaskProcessor(
		IServiceProvider serviceProvider,
		ILogger<RouletteTaskProcessor> logger)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

		foreach (var queue in _queuePool.Values)
			queue.StopProcess();
	}
}
