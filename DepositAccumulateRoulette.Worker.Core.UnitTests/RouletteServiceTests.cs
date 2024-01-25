using NSubstitute;
using NUnit.Framework.Legacy;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class RouletteServiceTests
{
	[Test]
	public async Task Play_Roulette()
	{
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new PlayRouletteCommandService(
			fakeShuffleService,
			fakeStatisticsService,
			fakeTgsRepository);

		var rouletteItems = new RouletteItem[]
		{
			new(Guid.Parse("8eb0f681-fe27-478f-9c1d-bdfdad8d3dfc"), "iphone 13 pro", 3M, 0M, 0M),
			new(Guid.Parse("3b776382-a909-4c4c-b739-4d7974eebb47"), "1000PHP", 3M, 0.25M, 1000M),
			new(Guid.Parse("aadd13cf-e1ea-4a53-9426-46f44cb7dbd4"),"300PHP", 3M, 0.75M, 300M)
		};

		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		var rouletteEventCountInfo = new RouletteEventCountInfo(
			playerCode,
			Guid.NewGuid(),
			DateOnly.Parse("2022-08-11"),
			4,
			3);

		var command = new PlayRouletteCommand(
			brandId,
			rouletteEventCountInfo,
			rouletteItems);

		_ = fakeStatisticsService.IncrementTotalDistributeAmountAsync(
			Arg.Is(command.EventInfo.PromotionId),
			Arg.Any<decimal>())
			.Returns(100M);

		_ = fakeShuffleService
			.ShuffleAsync(
				Arg.Is(command.EventInfo.PromotionId),
				Arg.Any<IEnumerable<RouletteItem>>())
			.Returns(new[] { 0, 1, 2 });

		_ = fakeStatisticsService.IncreaseRouletteUsedCountAsync(
				Arg.Is(command.EventInfo.PromotionId),
				Arg.Is(command.EventInfo.PlayerCode),
				Arg.Is(command.EventInfo.Date),
				Arg.Any<CancellationToken>())
			.Returns((true, 1));

		var actual = await sut.GetAsync(command);

		ClassicAssert.IsNotNull(actual);
		ClassicAssert.AreNotEqual(-1, actual.Result!.ItemIndex);

		_ = fakeTgsRepository
			.Received(1)
			.CreateRouletteRecordAsync(
				Arg.Any<CreateRouletteRecordInfo>());

		_ = fakeStatisticsService
			.Received(1)
			.IncrementTotalDistributeAmountAsync(
				Arg.Is(command.EventInfo.PromotionId),
				Arg.Any<decimal>(),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Play_Roulette_If_Run_Out_Of_Roulette_Count_Cannot_Play_Roulette()
	{
		var fakeShuffleService = Substitute.For<IShuffleService>();
		var fakeStatisticsService = Substitute.For<IStatisticsService>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var sut = new PlayRouletteCommandService(
			fakeShuffleService,
			fakeStatisticsService,
			fakeTgsRepository);

		var rouletteItems = new RouletteItem[]
		{
			new(Guid.Parse("8eb0f681-fe27-478f-9c1d-bdfdad8d3dfc"), "iphone 13 pro", 3M, 0M, 0M),
			new(Guid.Parse("3b776382-a909-4c4c-b739-4d7974eebb47"), "1000PHP", 3M, 0.25M, 1000M),
			new(Guid.Parse("aadd13cf-e1ea-4a53-9426-46f44cb7dbd4"),"300PHP", 3M, 0.75M, 300M)
		};

		var playerCode = "player1";
		var brandId = Guid.NewGuid();

		var rouletteEventCountInfo = new RouletteEventCountInfo(
			playerCode,
			Guid.NewGuid(),
			DateOnly.Parse("2022-08-11"),
			4,
			0);

		var command = new PlayRouletteCommand(
			brandId,
			rouletteEventCountInfo,
			rouletteItems);

		_ = fakeStatisticsService.IncreaseRouletteUsedCountAsync(
				Arg.Is(command.EventInfo.PromotionId),
				Arg.Is(command.EventInfo.PlayerCode),
				Arg.Is(command.EventInfo.Date),
				Arg.Any<CancellationToken>())
			.Returns((true, 5));

		var actual = await sut.GetAsync(command);

		ClassicAssert.IsNull(actual.Result);

		_ = fakeTgsRepository
			.DidNotReceive()
			.CreateRouletteRecordAsync(
				Arg.Is<CreateRouletteRecordInfo>(
					x =>
					x.PromotionId == command.EventInfo.PromotionId
					&& x.PlayerCode == command.EventInfo.PlayerCode
					&& x.EventDate == command.EventInfo.Date),
			Arg.Any<CancellationToken>());
	}
}
