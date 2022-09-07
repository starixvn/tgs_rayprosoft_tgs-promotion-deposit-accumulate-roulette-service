using System.Collections.Concurrent;
using Hangfire;
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
	private readonly ILogger<RouletteTaskQueue> _logger;
	private readonly BlockingCollection<RouletteTask> _rouletteTasks = new();

	private DateOnly? _eventDate;
	private int _usedCount;

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

			if (_player is null)
			{
				var playerServiceClient = scope.ServiceProvider.GetRequiredService<IPlayerServiceClient>();
				_player = await playerServiceClient.GetPlayerInfoAsync(PlayerCode, cancellationToken)
					.ConfigureAwait(false);
			}

			if (_promotion is null)
			{
				var rouletteServiceClient =
					scope.ServiceProvider.GetRequiredService<IDepositAccumulateRouletteServiceClient>();

				_promotion = await rouletteServiceClient
					.GetDepositAccumulateRoulettePromotionAsync(PromotionId, cancellationToken)
					.ConfigureAwait(false);
			}

			var rouletteItems = _promotion!.CampaignDetail.RouletteItems.ToArray();

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

			if (!_eventDate.HasValue || useEventInfo.Date != _eventDate)
			{
				var tgsRepository = scope.ServiceProvider.GetRequiredService<ITgsRepository>();
				var count = await tgsRepository
					.GetRouletteUsedCountAsync(PromotionId, PlayerCode, useEventInfo.Date, cancellationToken)
					.ConfigureAwait(false);

				_eventDate = useEventInfo.Date;
				_usedCount = count;
			}

			var playRouletteCommandService = scope.ServiceProvider
				.GetRequiredService<IGetService<PlayRouletteCommand, PlayRouletteResult?>>();

			var playResult = await playRouletteCommandService.GetAsync(
					new PlayRouletteCommand(_player!, useEventInfo, rouletteItems),
					cancellationToken)
				.ConfigureAwait(false);

			if (playResult is null)
			{
				var result = new RouletteResult(null, -1);
				rouletteTask.Promise.SetResult(result);
				return;
			}

			_usedCount += 1;

			var jobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();
			_ = jobClient.Enqueue<DistributeBonusBackgroundJob>(job
				=> job.ExecuteAsync(
					new DistributeBonusScheduleInfo(
						PromotionId,
						PlayerCode,
						useEventInfo.Date,
						_usedCount,
						playResult)));

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
}
