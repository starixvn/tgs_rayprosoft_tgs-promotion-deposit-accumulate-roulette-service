using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Tgs.PlayerService.Client;
using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class RouletteCommandQueue : IRouletteQueueStore, IDisposable
{
	private readonly IServiceProvider _serviceProvider;
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IDepositAccumulateRouletteServiceClient _rouletteServiceClient;

	private readonly BlockingCollection<IRouletteCommand> _rouletteCommands = [];

	private readonly DateOnly _today;

	private CancellationTokenSource? _cancellationTokenSource;
	private Task? _backgroundTask;
	private bool _disposedValue;
	private RouletteEventCountInfo? _useEvent;

	public DateTime Expired { get; private set; } = DateTime.Now.AddMinutes(1).AddSeconds(20);

	public string PlayerCode { get; }

	public Guid PromotionId { get; }

	public Guid BrandId { get; private set; }

	public IEnumerable<RouletteItem> RouletteItems { get; private set; } = Array.Empty<RouletteItem>();

	public bool IsStartProcess { get; private set; }

	public RouletteCommandQueue(
		string playerCode,
		Guid promotionId,
		IServiceProvider serviceProvider,
		IPlayerServiceClient playerServiceClient,
		IDepositAccumulateRouletteServiceClient rouletteServiceClient,
		ISystemClock systemClock)
	{
		PlayerCode = playerCode ?? throw new ArgumentNullException(nameof(playerCode));
		PromotionId = promotionId;

		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_rouletteServiceClient = rouletteServiceClient ?? throw new ArgumentNullException(nameof(rouletteServiceClient));

		if (systemClock is null)
			throw new ArgumentNullException(nameof(systemClock));

		_today = DateOnly.FromDateTime(systemClock.UtcNow.ToOffset(TimeSpan.FromHours(8D)).DateTime);
	}

	public void Enqueue(IRouletteCommand command)
		=> _rouletteCommands.Add(command);

	public async Task StartProcessAsync(CancellationToken cancellationToken)
	{
		if (IsStartProcess)
			return;

		var player = await _playerServiceClient
			.GetPlayerInfoAsync(PlayerCode, cancellationToken)
			.ConfigureAwait(false)
				  ?? throw new ArgumentNullException($"Player '{PlayerCode}' is not found");

		BrandId = player.BrandId;

		var promotion = await _rouletteServiceClient
			.GetDepositAccumulateRoulettePromotionAsync(
				PromotionId,
				cancellationToken)
			.ConfigureAwait(false)
					 ?? throw new ArgumentNullException($"Promotion '{PromotionId}' is not found");

		RouletteItems = promotion.CampaignDetail.RouletteItems;

		_cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_ = _cancellationTokenSource.Token.Register(() => _rouletteCommands.CompleteAdding());

		_backgroundTask = Task.Run(async () =>
		{
			foreach (var command in _rouletteCommands.GetConsumingEnumerable(_cancellationTokenSource.Token))
			{
				Expired = DateTime.Now.AddMinutes(1).AddSeconds(20);

				await using var scope = _serviceProvider.CreateAsyncScope();

				await command.ExecuteAsync(
						scope.ServiceProvider,
						this,
						_cancellationTokenSource.Token)
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
			_rouletteCommands.CompleteAdding();

			if (_rouletteCommands.TryGetNonEnumeratedCount(out var count)
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

	async ValueTask<RouletteEventCountInfo> IRouletteQueueStore.GetAvailableEventAsync(CancellationToken cancellationToken)
	{
		if (_useEvent is null || _useEvent.Date != _today && _useEvent.AvailableCount <= 0)
		{
			await using var scope = _serviceProvider.CreateAsyncScope();

			var eventService = scope.ServiceProvider.GetRequiredService<IDepositAccumulateRouletteEventService>();

			var eventInfo = await eventService.GetAvailableRouletteCountEventAsync(
					PromotionId,
					PlayerCode,
					cancellationToken)
				.ConfigureAwait(false);

			_useEvent = eventInfo ??
				   new RouletteEventCountInfo(
					   PlayerCode,
					   PromotionId,
					   _today,
					   0,
					   0);
		}

		return _useEvent;
	}

	ValueTask IRouletteQueueStore.UpdateEventUsedCountAsync(int usedCount, CancellationToken cancellationToken)
	{
		_useEvent = _useEvent! with { UsedCount = usedCount };

		return ValueTask.CompletedTask;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				if (_cancellationTokenSource?.IsCancellationRequested == false)
					_cancellationTokenSource?.Cancel();

				_rouletteCommands.CompleteAdding();
			}

			_disposedValue = true;
		}
	}
}
