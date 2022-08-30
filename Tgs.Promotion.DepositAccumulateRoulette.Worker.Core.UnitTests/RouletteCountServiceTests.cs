using NSubstitute;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class RouletteCountServiceTests
{
	[Test]
	public async Task Decrease_RouletteCount_When_Hit_Redis_Cache()
	{
		var fakeEventRepository = Substitute.For<IDepositAccumulateRouletteEventRepository>();
		var fakeCountRepository = Substitute.For<IRouletteCountRepository>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		var events = new DepositAccumulateRouletteEventInfo[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-10"),
				5,
				2400)
		};

		var expected = new RouletteCountInfo(events[0], 3, 2, 3, 2);

		_ = fakeEventRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(events.ToAsyncEnumerable());

		_ = fakeCountRepository.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(3);

		_ = fakeCountRepository.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(2);

		var sut = new RouletteCountService(fakeEventRepository, fakeCountRepository, fakeUsedCountQueryService);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Decrease_RouletteCount_When_Miss_Redis_Cache()
	{
		var fakeEventRepository = Substitute.For<IDepositAccumulateRouletteEventRepository>();
		var fakeCountRepository = Substitute.For<IRouletteCountRepository>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		var events = new DepositAccumulateRouletteEventInfo[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-11"),
				5,
				2400)
		};

		var expected = new RouletteCountInfo(events[0], 3, 2, 3, 2);

		_ = fakeEventRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(events.ToAsyncEnumerable());

		_ = fakeCountRepository.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns((int?)null);

		_ = fakeUsedCountQueryService.GetAsync(
				Arg.Is(new RouletteUsedCountQuery(
					promotionId,
					playerCode,
					events[0].Date)))
			.Returns(2);

		_ = fakeCountRepository
			.SetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date),
				Arg.Is(3))
			.Returns(true);

		_ = _ = fakeCountRepository
			.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(2);

		var sut = new RouletteCountService(fakeEventRepository, fakeCountRepository, fakeUsedCountQueryService);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Decrease_RouletteCount_When_Event_Data_Is_Empty()
	{
		var fakeEventRepository = Substitute.For<IDepositAccumulateRouletteEventRepository>();
		var fakeCountRepository = Substitute.For<IRouletteCountRepository>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		var expected = new RouletteCountInfo(null, 0, 0, 0, 0);

		_ = fakeEventRepository.GetDepositAccumulateRouletteEventInfosAsync(
					Arg.Is(promotionId),
					Arg.Is(playerCode),
					Arg.Any<CancellationToken>())
			.Returns(Array.Empty<DepositAccumulateRouletteEventInfo>().ToAsyncEnumerable());

		var sut = new RouletteCountService(fakeEventRepository, fakeCountRepository, fakeUsedCountQueryService);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);

		_ = fakeCountRepository
			.Received(0)
			.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<DateOnly>());
		;

		_ = fakeCountRepository
			.Received(0)
			.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<DateOnly>());
	}

	[Test]
	public async Task Decrease_RouletteCount_With_Two_Events_Returns()
	{
		var fakeEventRepository = Substitute.For<IDepositAccumulateRouletteEventRepository>();
		var fakeCountRepository = Substitute.For<IRouletteCountRepository>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		var events = new[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-10"),
				5,
				2400),
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-11"),
				4,
				1200)
		};

		var expected = new RouletteCountInfo(events[0], 3, 2, 7, 6);

		_ = fakeEventRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(events.ToAsyncEnumerable());

		_ = fakeCountRepository
			.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(3);

		_ = fakeCountRepository
				.GetRouletteCountAsync(
					Arg.Is(promotionId),
					Arg.Is(playerCode),
					Arg.Is(events[1].Date))
				.Returns(4);

		_ = fakeCountRepository
			.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(2);

		var sut = new RouletteCountService(fakeEventRepository, fakeCountRepository, fakeUsedCountQueryService);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public async Task Decrease_RouletteCount_With_Two_Events_Returns_And_First_Event_Has_Run_Out_Of_Roulette_Count()
	{
		var fakeEventRepository = Substitute.For<IDepositAccumulateRouletteEventRepository>();
		var fakeCountRepository = Substitute.For<IRouletteCountRepository>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		var events = new DepositAccumulateRouletteEventInfo[]
		{
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-10"),
				5,
				2400),
			new DepositAccumulateRouletteEventInfo(
				playerCode,
				promotionId,
				DateOnly.Parse("2022-08-11"),
				4,
				1200)
		};

		var expected = new RouletteCountInfo(events[1], 4, 3, 4, 3);

		_ = fakeEventRepository.GetDepositAccumulateRouletteEventInfosAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Any<CancellationToken>())
			.Returns(events.ToAsyncEnumerable());

		_ = fakeCountRepository
			.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[0].Date))
			.Returns(0);

		_ = fakeCountRepository
			.GetRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[1].Date))
			.Returns(4);

		_ = fakeCountRepository.DecreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(events[1].Date))
			.Returns(3);

		var sut = new RouletteCountService(fakeEventRepository, fakeCountRepository, fakeUsedCountQueryService);

		var actual = await sut.DecreaseRouletteCountAsync(promotionId, playerCode).ConfigureAwait(false);

		Assert.AreEqual(expected, actual);

		_ = fakeCountRepository
			.DidNotReceive()
			.IncreaseRouletteCountAsync(
				Arg.Is(promotionId),
					Arg.Is(playerCode),
					Arg.Is(events[0].Date));
	}

	[Test]
	public async Task Increase_RouletteCount()
	{
		var fakeEventRepository = Substitute.For<IDepositAccumulateRouletteEventRepository>();
		var fakeCountRepository = Substitute.For<IRouletteCountRepository>();
		var fakeUsedCountQueryService = Substitute.For<IGetService<RouletteUsedCountQuery, int>>();

		var promotionId = Guid.NewGuid();
		var playerCode = "player1";
		var date = DateOnly.Parse("2022-08-11");

		var sut = new RouletteCountService(fakeEventRepository, fakeCountRepository, fakeUsedCountQueryService);

		await sut.IncreaseRouletteCountAsync(promotionId, playerCode, date).ConfigureAwait(false);

		_ = fakeCountRepository
			.Received(1)
			.IncreaseRouletteCountAsync(
				Arg.Is(promotionId),
				Arg.Is(playerCode),
				Arg.Is(date));
	}
}
