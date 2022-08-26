using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class RouletteTaskQueue
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<RouletteTaskQueue> _logger;
	private readonly BlockingCollection<RouletteTask> _rouletteTasks = new();

	private CancellationTokenSource? _cancellationTokenSource;
	private Task? _backgroundTask;

	public string PlayerCode { get; }

	public bool IsStartProcess { get; private set; }

	public RouletteTaskQueue(
		string playerCode,
		IServiceProvider serviceProvider,
		ILogger<RouletteTaskQueue> logger)
	{
		PlayerCode = playerCode ?? throw new ArgumentNullException(nameof(playerCode));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public void Enqueue(RouletteTask task)
		=> _rouletteTasks.Add(task);

	public void StartProcess(CancellationToken cancellationToken)
	{
		if (IsStartProcess)
			return;

		IsStartProcess = true;

		_cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_cancellationTokenSource.Token.Register(() => _rouletteTasks.CompleteAdding());

		_backgroundTask = Task.Run(async () =>
		{
			foreach (var task in _rouletteTasks.GetConsumingEnumerable(_cancellationTokenSource.Token))
			{
				await HandleRouletteTaskAsync(task, _cancellationTokenSource.Token);
			}
		}, cancellationToken);
	}

	public void StopProcess()
	{
		if (_cancellationTokenSource?.IsCancellationRequested == false)
			_cancellationTokenSource?.Cancel();

		try
		{
			if (_rouletteTasks.TryGetNonEnumeratedCount(out var count)
				&& count > 0
				&& _backgroundTask?.IsCompleted == false)
			{
				_backgroundTask?.Wait();
			}
		}
		catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
		{
		}
	}

	private async Task HandleRouletteTaskAsync(RouletteTask rouletteTask, CancellationToken cancellationToken)
	{
		var promotionId = rouletteTask.Request.PromotionId;
		var playerCode = rouletteTask.Request.PlayerCode;

		try
		{
			if (cancellationToken.IsCancellationRequested)
			{
				rouletteTask.Promise.SetCanceled(cancellationToken);
				return;
			}

			await using var scope = _serviceProvider.CreateAsyncScope();

			var commandService =
				scope.ServiceProvider.GetRequiredService<IGetService<RouletteCommand, RouletteResult>>();

			var command = new RouletteCommand(promotionId, playerCode);

			var result = await commandService
				.GetAsync(command, cancellationToken)
				.ConfigureAwait(false);

			rouletteTask.Promise.SetResult(result);
		}
		catch (Exception ex)
		{
			_logger.LogError(
				new EventId(3000),
				ex,
				"Process Roulette with promotion({promotionId}) and player({playerCode}) occurs error.",
			promotionId, playerCode);

			rouletteTask.Promise.SetException(ex);
		}
	}
}
