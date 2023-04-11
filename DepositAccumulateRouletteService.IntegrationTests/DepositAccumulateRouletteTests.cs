extern alias RouletteSvc;

using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RouletteSvc::Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;
using Tgs.DepositAccumulateRoulette.Clients;
using Tgs.MessageQueue;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Tgs.Promotion.DepositAccumulateRoulette.Worker;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.Clients;
using Tgs.Promotion.DepositAccumulateRouletteService.ApplicationServices;
using Tgs.ServiceUtility;

using Client = Tgs.DepositAccumulateRoulette.Clients;

using EventDuring = Tgs.Promotion.DepositAccumulateRoulette.Worker.Clients.EventDuring;

using Worker = Tgs.Promotion.DepositAccumulateRoulette.Worker;

namespace Tgs.Promotion.DepositAccumulateRouletteService.IntegrationTests;

internal class DepositAccumulateRouletteTests
{
	[Test]
	public async Task Play_Roulette()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var fakeRedisConnectionManager = Substitute.For<IRedisConnectionManager>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeStoreRepository = Substitute.For<IStoreRepository>();
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRouletteServiceClient = Substitute.For<Worker.IDepositAccumulateRouletteServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeBackgroundJobClient = Substitute.For<IBackgroundJobClient>();

		await using var web = new TestDepositAccumulateRouletteServiceApplication(builder => builder
			.ConfigureServices(services =>
			{
				_ = services
					.AddSingleton(fakePromotionServiceClient)
					.AddSingleton(fakeRouletteServiceClient)
					.AddSingleton(fakePlayerServiceClient)
					.AddSingleton(fakeStoreRepository)
					.AddSingleton(fakeTgsRepository)
					.AddSingleton(fakeRedisConnectionManager)
					.AddSingleton(fakeSystemClock)
					.AddSingleton(fakeBackgroundJobClient);

				_ = services
					.AddSingleton<IShuffleService, ShuffleService>()
					.AddSingleton<IStatisticsService, StatisticsService>()
					.AddSingleton<IDistributePlanHasher, DistributePlanHasher>()
					.AddSingleton<IDepositAccumulateRouletteEventService, DepositAccumulateRouletteEventService>();

				_ = services
					.AddSingleton<ICommandService<DistributeBonusCommand>, DistributeBonusCommandService>()
					.AddSingleton<IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>,
						GetPromotionDistributePlanQueryService>()
					.AddSingleton<IGetService<PlayRouletteCommand, PlayRouletteResult>, PlayRouletteCommandService>()
					.AddSingleton<ICommandService<EnqueueDistributeBonusJobCommand>,
						EnqueueDistributeBonusJobCommandService>();

				_ = services
					.AddSingleton<RouletteCommandQueueManager>()
					.AddSingleton(sp =>
					{
						var manager = sp.GetRequiredService<RouletteCommandQueueManager>();
						var fakeRouletteHandleService = Substitute.For<IRouletteHandleService>();
						_ = fakeRouletteHandleService.RequestAsync(
								Arg.Any<RouletteTaskRequest>(),
								Arg.Any<CancellationToken>())
							.Returns(callInfo =>
							{
								var request = callInfo.Arg<RouletteTaskRequest>();

								return manager
									.RequestAsync(
										new RouletteCommandRequest(
											request.PromotionId,
											request.PlayerCode))
									.Result;
							});

						return fakeRouletteHandleService;
					});
			}));

		var serviceScope = web.Services.CreateAsyncScope();

		var sut = new DepositAccumulateRouletteClient(fakeMessageSender);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var playerCode = "player1";

		var depositItems = new DepositAccumulateItem[]
		{
			new(Guid.Parse("4398bd5f-cbd9-4982-a145-c98ceb4c5ea7"), 1000M, 1),
			new(Guid.Parse("c6149bd1-ac94-4e83-897e-434e1ea0fffa"), 2800M, 3)
		};

		var rouletteItems = new DepositAccumulateRoulette.Common.RouletteItem[]
		{
			new(Guid.Parse("8eb0f681-fe27-478f-9c1d-bdfdad8d3dfc"), "iphone 13 pro", 3M, 0M, 0M),
			new(Guid.Parse("3b776382-a909-4c4c-b739-4d7974eebb47"), "1000PHP", 3M, 0.25M, 1000M),
			new(Guid.Parse("aadd13cf-e1ea-4a53-9426-46f44cb7dbd4"),"300PHP", 3M, 0.75M, 300M)
		};

		var settings = new DepositAccumulateRouletteSettings(depositItems, rouletteItems);

		var now = DateTimeOffset.Now;
		_ = fakeSystemClock.UtcNow.Returns(now);

		var eventInfo = new DepositAccumulateRouletteEventInfo(
			playerCode,
			promotionId,
			DateOnly.FromDateTime(now.DateTime),
			4,
			2000);

		var promotionInfo = new PromotionMainInfo
		{
			Id = promotionId,
			EventDuring = (DateTime.UtcNow.AddDays(-10D), DateTime.UtcNow.AddDays(10D)),
			IsAutoSent = true,
			Name = "Test Deposit Accumulate Roulette",
			PromotionLimitAmount = null,
			PromotionType = "DepositAccumulateRoulette"
		};

		_ = fakeStoreRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<DateTimeOffset>())
			.Returns(new[] { eventInfo }.ToAsyncEnumerable());

		_ = fakeStoreRepository.GetRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<DateOnly>())
			.Returns(0);

		_ = fakeStoreRepository.IncreaseRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<DateOnly>())
			.Returns(1);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(new PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId,
				UserName = "Player",
				IdentityAuthentication = true,
				IsSetPayPassword = true,
				VipLevelId = Guid.NewGuid(),
				PhoneAuthentication = true,
				EmailAuthentication = false,
				RegisteredOn = DateTime.Now.AddDays(-10)
			});

		_ = fakePromotionServiceClient.GetPromotionMainInfoAsync(
				Arg.Is(promotionId),
				Arg.Any<CancellationToken>())
			.Returns(promotionInfo);

		_ = fakePromotionServiceClient.GetPromotionRuleAsync<GetDepositAccumulateRoulettePromotionQueryService.PromotionRule>(
				Arg.Is(promotionId),
				Arg.Any<CancellationToken>())
			.Returns(new GetDepositAccumulateRoulettePromotionQueryService.PromotionRule
			{
				CompaignDetail = settings
			});

		_ = fakeRouletteServiceClient.GetDepositAccumulateRoulettePromotionAsync(
				Arg.Is(promotionId),
				Arg.Any<CancellationToken>())
			.Returns(new DepositAccumulateRoulettePromotion(
				promotionInfo.Id,
				new EventDuring(promotionInfo.EventDuring.From, promotionInfo.EventDuring.To),
				promotionInfo.PromotionLimitAmount,
				settings));

		_ = fakeMessageSender.RequestAsync(
				Arg.Is("tgs.promotion.depositaccumulateroulette.roulette.play"),
				Arg.Any<ReadOnlyMemory<byte>>())
			.Returns(callInfo =>
			{
				var data = callInfo.Arg<ReadOnlyMemory<byte>>();

				var processor = ActivatorUtilities.CreateInstance<RouletteProcessor>(serviceScope.ServiceProvider);

				return processor.HandleAsync(data).Result;
			});

		var actual = await sut.RouletteAsync(promotionId, playerCode);

		Assert.IsNotNull(actual);
		Assert.AreNotEqual(Guid.Empty, actual.Id);
		Assert.AreNotEqual(-1, actual.ItemIndex);
	}

	[Test]
	public async Task Play_Roulette_Multiple_Times_With_Two_Available_Deposit_Distribute_Events()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeRedisConnectionManager = Substitute.For<IRedisConnectionManager>();

		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeStoreRepository = Substitute.For<IStoreRepository>();
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRouletteServiceClient = Substitute.For<Worker.IDepositAccumulateRouletteServiceClient>();
		var fakeBackgroundJobClient = Substitute.For<IBackgroundJobClient>();

		await using var web = new TestDepositAccumulateRouletteServiceApplication(builder => builder
			.ConfigureServices(services =>
			{
				_ = services
					.AddSingleton(fakePromotionServiceClient)
					.AddSingleton(fakeStoreRepository)
					.AddSingleton(fakePlayerServiceClient)
					.AddSingleton(fakeTgsRepository)
					.AddSingleton(fakeRedisConnectionManager)
					.AddSingleton(fakeSystemClock)
					.AddSingleton(fakeRouletteServiceClient)
					.AddSingleton(fakeBackgroundJobClient);

				_ = services
					.AddSingleton<IShuffleService, ShuffleService>()
					.AddSingleton<IStatisticsService, StatisticsService>()
					.AddSingleton<IDistributePlanHasher, DistributePlanHasher>()
					.AddSingleton<IDepositAccumulateRouletteEventService, DepositAccumulateRouletteEventService>();

				_ = services
					.AddSingleton<ICommandService<DistributeBonusCommand>, DistributeBonusCommandService>()
					.AddSingleton<IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>,
						GetPromotionDistributePlanQueryService>()
					.AddSingleton<IGetService<PlayRouletteCommand, PlayRouletteResult>, PlayRouletteCommandService>()
					.AddSingleton<ICommandService<EnqueueDistributeBonusJobCommand>,
						EnqueueDistributeBonusJobCommandService>();

				_ = services
					.AddSingleton<RouletteCommandQueueManager>()
					.AddSingleton(sp =>
					{
						var manager = sp.GetRequiredService<RouletteCommandQueueManager>();
						var fakeRouletteHandleService = Substitute.For<IRouletteHandleService>();
						_ = fakeRouletteHandleService.RequestAsync(
								Arg.Any<RouletteTaskRequest>(),
								Arg.Any<CancellationToken>())
							.Returns(callInfo =>
							{
								var request = callInfo.Arg<RouletteTaskRequest>();

								return manager
									.RequestAsync(
										new RouletteCommandRequest(
											request.PromotionId,
											request.PlayerCode))
									.Result;
							});

						return fakeRouletteHandleService;
					});
			}));

		var serviceScope = web.Services.CreateAsyncScope();

		var sut = new DepositAccumulateRouletteClient(fakeMessageSender);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var playerCode = "player1";

		var depositItems = new DepositAccumulateItem[]
		{
			new(Guid.Parse("4398bd5f-cbd9-4982-a145-c98ceb4c5ea7"), 1000M, 1),
			new(Guid.Parse("c6149bd1-ac94-4e83-897e-434e1ea0fffa"), 2800M, 3)
		};

		var rouletteItems = new DepositAccumulateRoulette.Common.RouletteItem[]
		{
			new(Guid.Parse("8eb0f681-fe27-478f-9c1d-bdfdad8d3dfc"), "iphone 13 pro", 3M, 0M, 0M),
			new(Guid.Parse("3b776382-a909-4c4c-b739-4d7974eebb47"), "1000PHP", 3M, 0.25M, 1000M),
			new(Guid.Parse("aadd13cf-e1ea-4a53-9426-46f44cb7dbd4"),"300PHP", 3M, 0.75M, 300M)
		};

		var now = DateTimeOffset.Parse("2022-09-14 07:30:00+08:00");

		var promotionInfo = new PromotionMainInfo
		{
			Id = promotionId,
			EventDuring = (DateTime.UtcNow.AddDays(-10D), DateTime.UtcNow.AddDays(10D)),
			IsAutoSent = true,
			Name = "Test Deposit Accumulate Roulette",
			PromotionLimitAmount = null,
			PromotionType = "DepositAccumulateRoulette"
		};

		var eventInfos = new[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-09-13"),
				2,
				2000),
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-09-14"),
				3,
				3100),
		};

		var settings = new DepositAccumulateRouletteSettings(depositItems, rouletteItems);

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeStoreRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<DateTimeOffset>())
			.Returns(eventInfos.ToAsyncEnumerable());

		_ = fakeStoreRepository.GetRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfos[0].Date))
			.Returns(0, 2);

		_ = fakeStoreRepository.GetRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfos[1].Date))
			.Returns(0);

		_ = fakeStoreRepository
			.IncreaseRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfos[0].Date))
			.Returns(1, 2);

		_ = fakeStoreRepository
			.IncreaseRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfos[1].Date))
			.Returns(1);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(new PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId,
				UserName = "Player",
				IdentityAuthentication = true,
				IsSetPayPassword = true,
				VipLevelId = Guid.NewGuid(),
				PhoneAuthentication = true,
				EmailAuthentication = false,
				RegisteredOn = DateTime.Now.AddDays(-10)
			});

		_ = fakePromotionServiceClient.GetPromotionMainInfoAsync(
				Arg.Is(promotionId),
				Arg.Any<CancellationToken>())
			.Returns(promotionInfo);

		_ = fakePromotionServiceClient.GetPromotionRuleAsync<GetDepositAccumulateRoulettePromotionQueryService.PromotionRule>(
				Arg.Is(promotionId),
				Arg.Any<CancellationToken>())
			.Returns(new GetDepositAccumulateRoulettePromotionQueryService.PromotionRule { CompaignDetail = settings });

		_ = fakeRouletteServiceClient.GetDepositAccumulateRoulettePromotionAsync(
				Arg.Is(promotionId),
				Arg.Any<CancellationToken>())
			.Returns(new DepositAccumulateRoulettePromotion(
				promotionInfo.Id,
				new EventDuring(promotionInfo.EventDuring.From, promotionInfo.EventDuring.To),
				promotionInfo.PromotionLimitAmount,
				settings));

		_ = fakeMessageSender.RequestAsync(
				Arg.Is("tgs.promotion.depositaccumulateroulette.roulette.play"),
				Arg.Any<ReadOnlyMemory<byte>>())
			.Returns(callInfo =>
			{
				var data = callInfo.Arg<ReadOnlyMemory<byte>>();

				var processor = ActivatorUtilities.CreateInstance<RouletteProcessor>(serviceScope.ServiceProvider);

				return processor.HandleAsync(data).Result;
			});

		for (var i = 0; i <= 2; i++)
		{
			var actual = await sut.RouletteAsync(promotionId, playerCode);

			Assert.IsNotNull(actual);
			Assert.AreNotEqual(Guid.Empty, actual.Id);
			Assert.AreNotEqual(-1, actual.ItemIndex);

			await Task.Delay(500);
		}

		_ = fakeTgsRepository
			.Received(2)
			.CreateRouletteRecordAsync(
				Arg.Is<CreateRouletteRecordInfo>(
					x =>
					x.PromotionId == promotionId
					&& x.PlayerCode == playerCode
					&& x.EventDate == eventInfos[0].Date),
				Arg.Any<CancellationToken>());

		_ = fakeTgsRepository
			.Received(1)
			.CreateRouletteRecordAsync(
				Arg.Is<CreateRouletteRecordInfo>(
					x =>
						x.PromotionId == promotionId
						&& x.PlayerCode == playerCode
						&& x.EventDate == eventInfos[1].Date),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Play_Roulette_Multiple_Times_With_All_Roulette_Counts_Are_Used()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeRedisConnectionManager = Substitute.For<IRedisConnectionManager>();

		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeStoreRepository = Substitute.For<IStoreRepository>();
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeRouletteServiceClient = Substitute.For<Worker.IDepositAccumulateRouletteServiceClient>();
		var fakeBackgroundJobClient = Substitute.For<IBackgroundJobClient>();

		await using var web = new TestDepositAccumulateRouletteServiceApplication(builder => builder
			.ConfigureServices(services =>
			{
				_ = services
					.AddSingleton(fakePromotionServiceClient)
					.AddSingleton(fakeStoreRepository)
					.AddSingleton(fakePlayerServiceClient)
					.AddSingleton(fakeTgsRepository)
					.AddSingleton(fakeRedisConnectionManager)
					.AddSingleton(fakeSystemClock)
					.AddSingleton(fakeRouletteServiceClient)
					.AddSingleton(fakeBackgroundJobClient);

				_ = services
					.AddSingleton<IShuffleService, ShuffleService>()
					.AddSingleton<IStatisticsService, StatisticsService>()
					.AddSingleton<IDistributePlanHasher, DistributePlanHasher>()
					.AddSingleton<IDepositAccumulateRouletteEventService, DepositAccumulateRouletteEventService>();

				_ = services
					.AddSingleton<ICommandService<DistributeBonusCommand>, DistributeBonusCommandService>()
					.AddSingleton<IGetService<GetPromotionDistributePlanQuery, PromotionDistributePlanInfo>,
						GetPromotionDistributePlanQueryService>()
					.AddSingleton<IGetService<PlayRouletteCommand, PlayRouletteResult>, PlayRouletteCommandService>()
					.AddSingleton<ICommandService<EnqueueDistributeBonusJobCommand>,
						EnqueueDistributeBonusJobCommandService>();

				_ = services
					.AddSingleton<RouletteCommandQueueManager>()
					.AddSingleton(sp =>
					{
						var manager = sp.GetRequiredService<RouletteCommandQueueManager>();
						var fakeRouletteHandleService = Substitute.For<IRouletteHandleService>();
						_ = fakeRouletteHandleService.RequestAsync(
								Arg.Any<RouletteTaskRequest>(),
								Arg.Any<CancellationToken>())
							.Returns(callInfo =>
							{
								var request = callInfo.Arg<RouletteTaskRequest>();

								return manager
									.RequestAsync(
										new RouletteCommandRequest(
											request.PromotionId,
											request.PlayerCode))
									.Result;
							});

						return fakeRouletteHandleService;
					});
			}));

		var serviceScope = web.Services.CreateAsyncScope();

		var sut = new DepositAccumulateRouletteClient(fakeMessageSender);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var playerCode = "player1";

		var depositItems = new DepositAccumulateItem[]
		{
			new(Guid.Parse("4398bd5f-cbd9-4982-a145-c98ceb4c5ea7"), 1000M, 1),
			new(Guid.Parse("c6149bd1-ac94-4e83-897e-434e1ea0fffa"), 2800M, 3)
		};

		var rouletteItems = new DepositAccumulateRoulette.Common.RouletteItem[]
		{
			new(Guid.Parse("8eb0f681-fe27-478f-9c1d-bdfdad8d3dfc"), "iphone 13 pro", 3M, 0M, 0M),
			new(Guid.Parse("3b776382-a909-4c4c-b739-4d7974eebb47"), "1000PHP", 3M, 0.25M, 1000M),
			new(Guid.Parse("aadd13cf-e1ea-4a53-9426-46f44cb7dbd4"),"300PHP", 3M, 0.75M, 300M)
		};

		var now = DateTimeOffset.Parse("2022-09-14 07:30:00+08:00");

		var promotionInfo = new PromotionMainInfo
		{
			Id = promotionId,
			EventDuring = (DateTime.UtcNow.AddDays(-10D), DateTime.UtcNow.AddDays(10D)),
			IsAutoSent = true,
			Name = "Test Deposit Accumulate Roulette",
			PromotionLimitAmount = null,
			PromotionType = "DepositAccumulateRoulette"
		};

		var eventInfos = new[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-09-13"),
				2,
				2000)
		};

		var settings = new DepositAccumulateRouletteSettings(depositItems, rouletteItems);

		_ = fakeSystemClock.UtcNow.Returns(now);

		_ = fakeStoreRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<DateTimeOffset>())
			.Returns(eventInfos.ToAsyncEnumerable());

		_ = fakeStoreRepository.GetRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfos[0].Date))
			.Returns(0, 2, 2);

		_ = fakeStoreRepository
			.IncreaseRouletteUsedCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(eventInfos[0].Date))
			.Returns(1, 2);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(new PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = brandId,
				UserName = "Player",
				IdentityAuthentication = true,
				IsSetPayPassword = true,
				VipLevelId = Guid.NewGuid(),
				PhoneAuthentication = true,
				EmailAuthentication = false,
				RegisteredOn = DateTime.Now.AddDays(-10)
			});

		_ = fakePromotionServiceClient.GetPromotionMainInfoAsync(
				Arg.Is(promotionId),
				Arg.Any<CancellationToken>())
			.Returns(promotionInfo);

		_ = fakePromotionServiceClient.GetPromotionRuleAsync<GetDepositAccumulateRoulettePromotionQueryService.PromotionRule>(
				Arg.Is(promotionId),
				Arg.Any<CancellationToken>())
			.Returns(new GetDepositAccumulateRoulettePromotionQueryService.PromotionRule { CompaignDetail = settings });

		_ = fakeRouletteServiceClient.GetDepositAccumulateRoulettePromotionAsync(
				Arg.Is(promotionId),
				Arg.Any<CancellationToken>())
			.Returns(new DepositAccumulateRoulettePromotion(
				promotionInfo.Id,
				new EventDuring(promotionInfo.EventDuring.From, promotionInfo.EventDuring.To),
				promotionInfo.PromotionLimitAmount,
				settings));

		_ = fakeMessageSender.RequestAsync(
				Arg.Is("tgs.promotion.depositaccumulateroulette.roulette.play"),
				Arg.Any<ReadOnlyMemory<byte>>())
			.Returns(callInfo =>
			{
				var data = callInfo.Arg<ReadOnlyMemory<byte>>();

				var processor = ActivatorUtilities.CreateInstance<RouletteProcessor>(serviceScope.ServiceProvider);

				return processor.HandleAsync(data).Result;
			});

		for (var i = 0; i <= 1; i++)
		{
			var actual = await sut.RouletteAsync(promotionId, playerCode);

			Assert.IsNotNull(actual);
			Assert.AreNotEqual(Guid.Empty, actual.Id);
			Assert.AreNotEqual(-1, actual.ItemIndex);

			await Task.Delay(500);
		}

		var lastActual = await sut.RouletteAsync(promotionId, playerCode);
		Assert.IsNotNull(lastActual);
		Assert.IsNull(lastActual.Id);
		Assert.AreEqual(-1, lastActual.ItemIndex);

		_ = fakeTgsRepository
			.Received(2)
			.CreateRouletteRecordAsync(
				Arg.Is<CreateRouletteRecordInfo>(
					x =>
						x.PromotionId == promotionId
						&& x.PlayerCode == playerCode
						&& x.EventDate == eventInfos[0].Date),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Query_Roulette_Records_By_PlayerCode_And_PromotionId()
	{
		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var fakeWorkerClient = Substitute.For<IDepositAccumulateRouletteWorkerClient>();

		await using var web = new TestDepositAccumulateRouletteServiceApplication(builder =>
			builder.ConfigureServices(services => _ = services.AddSingleton(fakeWorkerClient)));

		var httpClient = web.CreateClient();

		var sut = new Client.DepositAccumulateRouletteServiceClient(httpClient);

		_ = await sut.GetRouletteRecordsAsync(promotionId, playerCode)
			.ToArrayAsync()
			.ConfigureAwait(false);

		_ = fakeWorkerClient
			.Received(1)
			.QueryRouletteRecordsAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Query_Roulette_Records()
	{
		var brandId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();
		var playerCode = "player1";

		var fakeWorkerClient = Substitute.For<IDepositAccumulateRouletteWorkerClient>();

		await using var web = new TestDepositAccumulateRouletteServiceApplication(builder =>
			builder.ConfigureServices(services => _ = services.AddSingleton(fakeWorkerClient)));

		var httpClient = web.CreateClient();

		var sut = new Client.DepositAccumulateRouletteServiceClient(httpClient);

		var query = new RouletteRecordQuery
		{
			PromotionIds = new[] { promotionId },
			PlayerCodes = new[] { playerCode },
			BeginTime = DateTimeOffset.Parse("2022-10-26T00:00:00+00:00"),
			EndTime = DateTimeOffset.Parse("2022-10-28T00:00:00+00:00")
		};

		_ = await sut
			.GetRouletteRecordsAsync(brandId, query)
			.ToArrayAsync()
			.ConfigureAwait(false);

		_ = fakeWorkerClient
			.Received(1)
			.QueryRouletteRecordsAsync(
				Arg.Is(brandId),
				Arg.Is<RouletteRecordQueryConditions>(condition
					=> condition.PromotionIds.SequenceEqual(query.PromotionIds)
					   && condition.PlayerCodes.SequenceEqual(query.PlayerCodes)
					   && condition.TimeRange.Begin == query.BeginTime
					   && condition.TimeRange.End == query.EndTime),
				Arg.Any<Paging>(),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Get_Roulette_Record()
	{
		var recordId = Guid.NewGuid();
		var brandId = Guid.NewGuid();

		var fakeWorkerClient = Substitute.For<IDepositAccumulateRouletteWorkerClient>();

		await using var web = new TestDepositAccumulateRouletteServiceApplication(builder =>
			builder.ConfigureServices(services => _ = services.AddSingleton(fakeWorkerClient)));

		var httpClient = web.CreateClient();

		var sut = new Client.DepositAccumulateRouletteServiceClient(httpClient);

		_ = await sut.GetRouletteRecordAsync(brandId, recordId)
			.ConfigureAwait(false);

		_ = fakeWorkerClient
			.Received(1)
			.GetRouletteRecordAsync(
			Arg.Is(brandId),
			Arg.Is(recordId),
			Arg.Any<CancellationToken>());
	}
}
