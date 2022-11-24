using ExpectedObjects;
using NSubstitute;
using Tgs.PlayerService.Client;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class PromotionQueryTests
{
	[Test]
	public async Task Get_Player_Roulette_Promotions_When_Count_Is_Stored()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeStoreRepository = Substitute.For<IStoreRepository>();
		var fakeEventService = Substitute.For<IDepositAccumulateRouletteEventService>();
		var fakePromotionService = Substitute.For<IPromotionService>();

		var sut = new GetRouletteCountItemsQueryService(
			fakePromotionService,
			fakeEventService,
			fakeTgsRepository,
			fakeStoreRepository);

		var playerInfo = new PlayerInfo
		{
			PlayerCode = "player1",
			BrandId = Guid.NewGuid(),
			UserName = "player1"
		};

		var firstPromotionId = Guid.NewGuid();
		var secondPromotionId = Guid.NewGuid();

		var firstEventInfo = new DepositAccumulateRouletteEventInfo(
			playerInfo.PlayerCode,
			firstPromotionId,
			DateOnly.Parse("2022-09-01"),
			5,
			4800);

		var secondEventInfo = new DepositAccumulateRouletteEventInfo(
			playerInfo.PlayerCode,
			secondPromotionId,
			DateOnly.Parse("2022-09-01"),
			4,
			3600);

		var expected = new[]
		{
			new RouletteCountItemInfo(firstPromotionId, 4),
			new RouletteCountItemInfo(secondPromotionId, 2)
		};

		var query = new GetRouletteCountItemsQuery(playerInfo.PlayerCode);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(query.PlayerCode))
			.Returns(playerInfo);

		_ = fakePromotionService.GetPlayerAvailablePromotionIdsAsync(Arg.Is(playerInfo.PlayerCode),
				Arg.Any<CancellationToken>())
			.Returns(new[] { firstPromotionId, secondPromotionId }.ToAsyncEnumerable());

		_ = fakeEventService
			.QueryDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(firstPromotionId),
				Arg.Is(playerInfo.PlayerCode),
				Arg.Any<CancellationToken>())
			.Returns(new[] { firstEventInfo }.ToAsyncEnumerable());

		_ = fakeEventService.QueryDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(secondPromotionId),
				Arg.Is(playerInfo.PlayerCode),
				Arg.Any<CancellationToken>())
				.Returns(new[] { secondEventInfo }.ToAsyncEnumerable());

		_ = fakeStoreRepository
			.GetRouletteUsedCountAsync(
				Arg.Is(firstEventInfo.PromotionId),
				Arg.Is(firstEventInfo.PlayerCode),
				Arg.Is(firstEventInfo.Date))
			.Returns(1);

		_ = fakeStoreRepository
			.GetRouletteUsedCountAsync(
				Arg.Is(secondEventInfo.PromotionId),
				Arg.Is(secondEventInfo.PlayerCode),
				Arg.Is(secondEventInfo.Date))
			.Returns(2);

		var actual = await sut.QueryAsync(query).ToArrayAsync();

		CollectionAssert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Get_Player_Roulette_Promotions_When_Count_Is_Not_Stored()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeStoreRepository = Substitute.For<IStoreRepository>();
		var fakeEventService = Substitute.For<IDepositAccumulateRouletteEventService>();
		var fakePromotionService = Substitute.For<IPromotionService>();

		var sut = new GetRouletteCountItemsQueryService(
			fakePromotionService,
			fakeEventService,
			fakeTgsRepository,
			fakeStoreRepository);

		var playerInfo = new PlayerInfo
		{
			PlayerCode = "player1",
			BrandId = Guid.NewGuid(),
			UserName = "player1"
		};

		var promotionId = Guid.NewGuid();

		var eventInfo = new DepositAccumulateRouletteEventInfo(
			playerInfo.PlayerCode,
			promotionId,
			DateOnly.Parse("2022-09-01"),
			5,
			4800);

		var usedCount = 2;

		var expected = new[]
		{
			new RouletteCountItemInfo(promotionId, 3)
		};

		var query = new GetRouletteCountItemsQuery(playerInfo.PlayerCode);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(query.PlayerCode))
			.Returns(playerInfo);

		_ = fakePromotionService.GetPlayerAvailablePromotionIdsAsync(
				Arg.Is(playerInfo.PlayerCode),
				Arg.Any<CancellationToken>())
			.Returns(new[] { promotionId }.ToAsyncEnumerable());

		_ = fakeEventService
			.QueryDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerInfo.PlayerCode),
				Arg.Any<CancellationToken>())
			.Returns(new[] { eventInfo }.ToAsyncEnumerable());

		_ = fakeStoreRepository
			.GetRouletteUsedCountAsync(
				Arg.Is(eventInfo.PromotionId),
				Arg.Is(eventInfo.PlayerCode),
				Arg.Is(eventInfo.Date))
			.Returns((int?)null);

		_ = fakeTgsRepository
			.GetRouletteUsedCountAsync(
				Arg.Is(eventInfo.PromotionId),
				Arg.Is(eventInfo.PlayerCode),
				Arg.Is(eventInfo.Date))
			.Returns(usedCount);

		var actual = await sut.QueryAsync(query).ToArrayAsync();

		CollectionAssert.AreEqual(expected, actual);

		_ = fakeStoreRepository
			.SetRouletteUsedCountAsync(Arg.Is(eventInfo.PromotionId),
			Arg.Is(eventInfo.PlayerCode),
			Arg.Is(eventInfo.Date),
			Arg.Is(usedCount));
	}

	[Test]
	public async Task Get_Player_Available_Promotions_When_Promotion_Is_Stored()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeStoreRepository = Substitute.For<IStoreRepository>();

		var playerCode = "player1";

		var now = new DateTimeOffset(2022, 9, 1, 8, 0, 0, TimeSpan.FromHours(8));
		var date = DateOnly.FromDateTime(now.DateTime);

		var firstPromotionId = Guid.NewGuid();
		var secondPromotionId = Guid.NewGuid();

		var expected = new[] { firstPromotionId, secondPromotionId };

		var sut = new PromotionService(
			fakeSystemClock,
			fakeTgsRepository,
			fakeStoreRepository);

		_ = fakeSystemClock.UtcNow.Returns(now.ToUniversalTime());

		_ = fakeStoreRepository.GetPlayerAvailablePromotionIdsAsync(
			Arg.Is(playerCode),
			Arg.Is(date))
			.Returns(expected.ToAsyncEnumerable());

		var actual = await sut.GetPlayerAvailablePromotionIdsAsync(playerCode).ToArrayAsync();

		CollectionAssert.AreEqual(expected, actual);

		_ = fakeTgsRepository
			.DidNotReceive()
			.GetPlayerAvailablePromotionIdsAsync(
				Arg.Is(playerCode),
				Arg.Is(date));
	}

	[Test]
	public async Task Get_Player_Available_Promotions_When_Promotion_Is_Not_Stored()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeStoreRepository = Substitute.For<IStoreRepository>();

		var playerCode = "player1";

		var now = new DateTimeOffset(2022, 9, 1, 8, 0, 0, TimeSpan.FromHours(8));
		var date = DateOnly.FromDateTime(now.DateTime);

		var firstPromotionId = Guid.NewGuid();
		var secondPromotionId = Guid.NewGuid();

		var expected = new[] { firstPromotionId, secondPromotionId };

		var sut = new PromotionService(
			fakeSystemClock,
			fakeTgsRepository,
			fakeStoreRepository);

		_ = fakeSystemClock.UtcNow.Returns(now.ToUniversalTime());

		_ = fakeStoreRepository.GetPlayerAvailablePromotionIdsAsync(
				Arg.Is(playerCode),
				Arg.Is(date))
			.Returns(Array.Empty<Guid>().ToAsyncEnumerable());

		_ = fakeTgsRepository.GetPlayerAvailablePromotionIdsAsync(
				Arg.Is(playerCode),
				Arg.Is(date))
			.Returns(expected.ToAsyncEnumerable());

		var actual = await sut.GetPlayerAvailablePromotionIdsAsync(playerCode).ToArrayAsync();

		CollectionAssert.AreEqual(expected, actual);

		_ = fakeStoreRepository
			.Received(1)
			.SetPlayerAvailablePromotionIdsAsync(
				Arg.Is(playerCode),
				Arg.Is(date),
				Arg.Is<IEnumerable<Guid>>(x
					=> expected.ToExpectedObject().Matches(x)),
				Arg.Any<TimeSpan>());
	}
}
