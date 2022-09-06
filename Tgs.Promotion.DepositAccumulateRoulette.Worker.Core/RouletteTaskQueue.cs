using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class RouletteTaskQueue : IDisposable
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<RouletteTaskQueue> _logger;
	private readonly BlockingCollection<RouletteTask> _rouletteTasks = new();
	private readonly Dictionary<DateOnly, int> _rouletteUsedCounts = new();

	private CancellationTokenSource? _cancellationTokenSource;
	private Task? _backgroundTask;
	private bool _disposedValue;

	public DateTime Expired { get; private set; } = DateTime.Now.AddMinutes(1).AddSeconds(20);

	public string PlayerCode { get; }

	public Guid PromotionId { get; }

	public bool IsStartProcess { get; private set; }

	public RouletteTaskQueue(
		string playerCode,
		Guid promotionId,
		IServiceProvider serviceProvider,
		ILogger<RouletteTaskQueue> logger)
	{
		PlayerCode = playerCode ?? throw new ArgumentNullException(nameof(playerCode));
		PromotionId = promotionId;

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
				Expired = DateTime.Now.AddMinutes(1).AddSeconds(20);

				await HandleRouletteTaskAsync(task, _cancellationTokenSource.Token)
					.ConfigureAwait(false);
			}
		}, cancellationToken);
	}

	public void StopProcess()
	{
		if (_cancellationTokenSource?.IsCancellationRequested == false)
			_cancellationTokenSource?.Cancel();

		try
		{
			_rouletteTasks.CompleteAdding();

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

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				if (_cancellationTokenSource?.IsCancellationRequested == false)
					_cancellationTokenSource?.Cancel();

				_rouletteTasks.CompleteAdding();
			}

			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			_disposedValue = true;
		}
	}

	private async Task HandleRouletteTaskAsync(RouletteTask rouletteTask, CancellationToken cancellationToken)
	{
		try
		{
			if (cancellationToken.IsCancellationRequested)
			{
				rouletteTask.Promise.SetCanceled(cancellationToken);
				return;
			}

			await using var scope = _serviceProvider.CreateAsyncScope();

			var playerServiceClient = scope.ServiceProvider.GetRequiredService<IPlayerServiceClient>();
			var player = await playerServiceClient.GetPlayerInfoAsync(PlayerCode, cancellationToken)
				.ConfigureAwait(false);

			var rouletteServiceClient =
				scope.ServiceProvider.GetRequiredService<IDepositAccumulateRouletteServiceClient>();

			var promotionSetting = await rouletteServiceClient
				.GetDepositAccumulateRoulettePromotionAsync(PromotionId, cancellationToken)
				.ConfigureAwait(false);

			var rouletteItems = promotionSetting!.CampaignDetail.RouletteItems.ToArray();

			var getAvailableRouletteEventService = scope.ServiceProvider
				.GetRequiredService<IGetService<GetAvailableRouletteCountEventQuery, RouletteEventCountInfo?>>();

			var useEventInfo = await getAvailableRouletteEventService
				.GetAsync(
					new GetAvailableRouletteCountEventQuery(PromotionId, PlayerCode),
					cancellationToken)
				.ConfigureAwait(false);

			if (useEventInfo is null)
			{
				var result = new RouletteResult(null, -1);
				rouletteTask.Promise.SetResult(result);
				return;
			}

			if (!_rouletteUsedCounts.ContainsKey(useEventInfo.Date))
			{
				await InitUsedCountAsync(useEventInfo.Date, scope.ServiceProvider, cancellationToken)
					.ConfigureAwait(false);
			}

			var playRouletteCommandService = scope.ServiceProvider
				.GetRequiredService<IGetService<PlayRouletteCommand, PlayRouletteResult?>>();

			var playResult = await playRouletteCommandService.GetAsync(
					new PlayRouletteCommand(player!, useEventInfo, rouletteItems),
					cancellationToken)
				.ConfigureAwait(false);

			if (playResult is null)
			{
				var result = new RouletteResult(null, -1);
				rouletteTask.Promise.SetResult(result);
				return;
			}

			var usedCount = IncreaseUsedCount(useEventInfo.Date);

			_ = Task.Run(async () =>
			{
				var promotionServiceClient = scope.ServiceProvider
					.GetRequiredService<IPromotionServiceClient>();
				await promotionServiceClient.UpdatePromotionUsedAmountAsync(
					PromotionId,
					playResult.TotalAmount,
					playResult.RecordId).ConfigureAwait(false);

				var distributeService = scope.ServiceProvider
					.GetRequiredService<ICommandService<DistributeBonusCommand>>();
				await distributeService.ExecuteAsync(new DistributeBonusCommand(
					PromotionId,
					PlayerCode,
					useEventInfo.Date,
					usedCount,
					playResult.Bonus,
					playResult.Bucket)).ConfigureAwait(false);
			});

			var rouletteResult = new RouletteResult(playResult.RouletteId, playResult.ItemIndex);
			rouletteTask.Promise.SetResult(rouletteResult);
		}
		catch (Exception ex)
		{
			_logger.LogError(
				new EventId(3000),
				ex,
				"Process Roulette with promotion({promotionId}) and player({playerCode}) occurs error.",
			PromotionId, PlayerCode);

			rouletteTask.Promise.SetException(ex);
		}
	}

	private async Task InitUsedCountAsync(DateOnly date, IServiceProvider serviceProvider, CancellationToken cancellationToken)
	{
		var tgsRepository = serviceProvider.GetRequiredService<ITgsRepository>();
		var usedCount = await tgsRepository
			.GetRouletteUsedCountAsync(PromotionId, PlayerCode, date, cancellationToken)
			.ConfigureAwait(false);

		lock (_rouletteUsedCounts)
		{
			_rouletteUsedCounts.TryAdd(date, usedCount);
		}
	}

	private int IncreaseUsedCount(DateOnly date)
	{
		lock (_rouletteUsedCounts)
		{
			var lastCount = _rouletteUsedCounts[date];
			var usedCount = lastCount + 1;

			_rouletteUsedCounts[date] = usedCount;

			return usedCount;
		}
	}
}
