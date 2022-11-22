using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.PlayerService.Client;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class RouletteTaskQueue : IDisposable
{
	private readonly IServiceProvider _serviceProvider;
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IDepositAccumulateRouletteServiceClient _rouletteServiceClient;

	private readonly ILogger<RouletteTaskQueue> _logger;
	private readonly BlockingCollection<RouletteTask> _rouletteTasks = new();

	private readonly DateOnly _today;

	private RouletteEventCountInfo? _useEvent;

	private PlayerInfo? _player;
	private DepositAccumulateRoulettePromotion? _promotion;

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
		IPlayerServiceClient playerServiceClient,
		IDepositAccumulateRouletteServiceClient rouletteServiceClient,
		ISystemClock systemClock,
		ILogger<RouletteTaskQueue> logger)
	{
		PlayerCode = playerCode ?? throw new ArgumentNullException(nameof(playerCode));
		PromotionId = promotionId;

		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_rouletteServiceClient = rouletteServiceClient ?? throw new ArgumentNullException(nameof(rouletteServiceClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

		if (systemClock is null)
			throw new ArgumentNullException(nameof(systemClock));

		_today = DateOnly.FromDateTime(systemClock.UtcNow.ToOffset(TimeSpan.FromHours(8D)).DateTime);
	}

	public void Enqueue(RouletteTask task)
		=> _rouletteTasks.Add(task);

	public async Task StartProcessAsync(CancellationToken cancellationToken)
	{
		if (IsStartProcess)
			return;

		_player = await _playerServiceClient
			.GetPlayerInfoAsync(PlayerCode, cancellationToken)
			.ConfigureAwait(false)
				  ?? throw new ArgumentNullException($"Player '{PlayerCode}' is not found");

		_promotion = await _rouletteServiceClient
			.GetDepositAccumulateRoulettePromotionAsync(
				PromotionId,
				cancellationToken)
			.ConfigureAwait(false)
					 ?? throw new ArgumentNullException($"Promotion '{PromotionId}' is not found");

		_cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_ = _cancellationTokenSource.Token.Register(() => _rouletteTasks.CompleteAdding());

		_backgroundTask = Task.Run(async () =>
		{
			foreach (var task in _rouletteTasks.GetConsumingEnumerable(_cancellationTokenSource.Token))
			{
				Expired = DateTime.Now.AddMinutes(1).AddSeconds(20);

				await HandleRouletteTaskAsync(task, _cancellationTokenSource.Token)
					.ConfigureAwait(false);
			}
		}, cancellationToken);

		IsStartProcess = true;
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
			var serviceCoordinator = scope.ServiceProvider.GetRequiredService<IServiceCoordinator>();

			var rouletteItems = _promotion!.CampaignDetail.RouletteItems.ToArray();

			if (_useEvent is null || _useEvent.Date != _today && _useEvent.AvailableCount <= 0)
			{
				_useEvent = await GetAvailableEventAsync(serviceCoordinator, cancellationToken)
					.ConfigureAwait(false);
			}

			if (_useEvent.DistributeCount == 0 || _useEvent.AvailableCount <= 0)
			{
				var result = new RouletteResult(null, -1);
				rouletteTask.Promise.SetResult(result);
				return;
			}

			var playRouletteResult = await serviceCoordinator.GetAsync<PlayRouletteCommand, PlayRouletteResult>(
					new PlayRouletteCommand(_player!, _useEvent, rouletteItems),
					cancellationToken)
				.ConfigureAwait(false);

			if (playRouletteResult.Result is null)
			{
				var result = new RouletteResult(null, -1);
				rouletteTask.Promise.SetResult(result);
				return;
			}

			_useEvent = _useEvent with { UsedCount = playRouletteResult.UsedCount };

			await serviceCoordinator.ExecuteAsync(
					new EnqueueDistributeBonusJobCommand(
						playRouletteResult.Result.RecordId,
						playRouletteResult.Result.TotalDistributeAmount),
					cancellationToken)
				.ConfigureAwait(false);

			var rouletteResult = new RouletteResult(playRouletteResult.Result.RouletteId, playRouletteResult.Result.ItemIndex);
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

	private async ValueTask<RouletteEventCountInfo> GetAvailableEventAsync(
		IServiceCoordinator serviceCoordinator,
		CancellationToken cancellationToken)
	{
		var eventInfo = await serviceCoordinator
			.GetAsync<GetAvailableRouletteCountEventQuery, RouletteEventCountInfo?>(
				new GetAvailableRouletteCountEventQuery(PromotionId, PlayerCode),
				cancellationToken)
			.ConfigureAwait(false);

		return eventInfo ??
			   new RouletteEventCountInfo(
				   PlayerCode,
				   PromotionId,
				   _today,
				   0,
				   0);
	}
}
