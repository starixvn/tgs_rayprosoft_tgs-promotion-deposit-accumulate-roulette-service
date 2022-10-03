using NSubstitute;
using Tgs.Promotion.DepositAccumulateRoulette.Background.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.Core.UnitTests;

internal class DepositAccumulatedCalculateCommandServiceUnitTests
{
	[Test]
	public async Task Can_Calculate_By_Daily()
	{
		var fakeBackgroundRepository = Substitute.For<IBackgroundRepository>();
		var serializer = new DepositAccumulateRouletteSerializer();

		var settings = new DepositAccumulateRouletteSettings(
			new DepositAccumulateItem[]
			{
				new(Guid.Parse("ab85fad0-fa30-46ef-9963-693fdbe8deb5"), 1000M, 1)
			},
			new RouletteItem[]
			{
				new(Guid.Parse("1cd30c0a-a61c-49fc-ba95-0ba78bfbcbb1"), "iphone 13 pro", 3M, 0M, 0M)
			});

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();

		var promotions = new PromotionInfo[]
		{
			new(promotionId,
				new DateTime(2022, 7, 25, 16, 0, 0, DateTimeKind.Utc),
				new DateTime(2022, 7, 31, 16, 0, 0, DateTimeKind.Utc),
				brandId,
				serializer.Serialize(settings))
		};

		var depositForms = new DepositFormInfo[]
		{
			new(Guid.NewGuid(),
				"Player1",
				brandId,
				promotionId,
				1000,
				1000),
			new(Guid.NewGuid(),
				"Player2",
				brandId,
				promotionId,
				1200,
				1300),
		};

		var expected = new[]
		{
			new DepositAccumulateRouletteEventAddInfo(
				"Player1",
				brandId,
				promotionId,
				1,
				1000),
			new DepositAccumulateRouletteEventAddInfo(
				"Player2",
				brandId,
				promotionId,
				1,
				1200)
		};

		var command = new DistributeDepositAccumulateRouletteCountCommand(DateOnly.Parse("2022-07-29"));

		_ = fakeBackgroundRepository
			.GetValidDepositAccumulateRoulettePromotionsAsync(
				Arg.Is(command.ProcessDate),
				Arg.Any<CancellationToken>())
			.Returns(promotions.ToAsyncEnumerable());

		_ = fakeBackgroundRepository.GetApprovedDepositFormsAsync(
				Arg.Is<IEnumerable<Guid>>(x => x.Contains(brandId)),
				Arg.Is(command.ProcessDate),
				Arg.Any<CancellationToken>())
			.Returns(depositForms.ToAsyncEnumerable());

		var sut = new DistributeDepositAccumulateRouletteCountCommandService(fakeBackgroundRepository, serializer);

		var actual = await sut.QueryAsync(command).ToArrayAsync();

		CollectionAssert.AreEqual(expected, actual);

		_ = fakeBackgroundRepository
			   .Received(1)
			   .AddDepositAccumulateRouletteEventsAsync(
				   Arg.Is(command.ProcessDate),
				   Arg.Any<IEnumerable<DepositAccumulateRouletteEventAddInfo>>(),
				   Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Not_Calculate_By_Daily_Without_Promotion()
	{
		var fakeBackgroundRepository = Substitute.For<IBackgroundRepository>();
		var serializer = new DepositAccumulateRouletteSerializer();

		var promotions = Array.Empty<PromotionInfo>();

		var command = new DistributeDepositAccumulateRouletteCountCommand(DateOnly.Parse("2022-07-29"));

		_ = fakeBackgroundRepository
			.GetValidDepositAccumulateRoulettePromotionsAsync(
				Arg.Is(command.ProcessDate), Arg.Any<CancellationToken>())
			.Returns(promotions.ToAsyncEnumerable());

		var sut = new DistributeDepositAccumulateRouletteCountCommandService(fakeBackgroundRepository, serializer);

		var actual = await sut.QueryAsync(command).ToArrayAsync();

		Assert.AreEqual(0, actual.Length);

		_ = fakeBackgroundRepository
			.DidNotReceive()
			.GetApprovedDepositFormsAsync(
				Arg.Any<IEnumerable<Guid>>(),
				Arg.Is(command.ProcessDate),
				Arg.Any<CancellationToken>());

		_ = fakeBackgroundRepository
			.DidNotReceive()
			.AddDepositAccumulateRouletteEventsAsync(
				Arg.Is(command.ProcessDate),
				Arg.Any<IEnumerable<DepositAccumulateRouletteEventAddInfo>>(),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Not_Calculate_By_Daily_Without_Approved_DepositForms()
	{
		var fakeBackgroundRepository = Substitute.For<IBackgroundRepository>();
		var serializer = new DepositAccumulateRouletteSerializer();

		var settings = new DepositAccumulateRouletteSettings(
			new DepositAccumulateItem[]
			{
				new(Guid.Parse("ab85fad0-fa30-46ef-9963-693fdbe8deb5"), 1000M, 1)
			},
			new RouletteItem[]
			{
				new(Guid.Parse("1cd30c0a-a61c-49fc-ba95-0ba78bfbcbb1"), "iphone 13 pro", 3M, 0M, 0M)
			});

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();

		var promotions = new PromotionInfo[]
		{
			new(promotionId,
				new DateTime(2022, 7, 25, 16, 0, 0, DateTimeKind.Utc),
				new DateTime(2022, 7, 31, 16, 0, 0, DateTimeKind.Utc),
				brandId,
				serializer.Serialize(settings))
		};

		var depositForms = Array.Empty<DepositFormInfo>();

		var command = new DistributeDepositAccumulateRouletteCountCommand(DateOnly.Parse("2022-07-29"));

		_ = fakeBackgroundRepository
			.GetValidDepositAccumulateRoulettePromotionsAsync(
				Arg.Is(command.ProcessDate),
				Arg.Any<CancellationToken>())
			.Returns(promotions.ToAsyncEnumerable());

		_ = fakeBackgroundRepository.GetApprovedDepositFormsAsync(
				Arg.Is<IEnumerable<Guid>>(x => x.Contains(brandId)),
				Arg.Is(command.ProcessDate),
				Arg.Any<CancellationToken>())
			.Returns(depositForms.ToAsyncEnumerable());

		var sut = new DistributeDepositAccumulateRouletteCountCommandService(fakeBackgroundRepository, serializer);

		var actual = await sut.QueryAsync(command).ToArrayAsync();

		Assert.AreEqual(0, actual.Length);

		_ = fakeBackgroundRepository
			.DidNotReceive()
			.AddDepositAccumulateRouletteEventsAsync(
				Arg.Is(command.ProcessDate),
				Arg.Any<IEnumerable<DepositAccumulateRouletteEventAddInfo>>(),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Skip_Calculate_By_Daily_Without_Promotion_Setting_Is_Null()
	{
		var fakeBackgroundRepository = Substitute.For<IBackgroundRepository>();
		var serializer = new DepositAccumulateRouletteSerializer();

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();

		var promotions = new PromotionInfo[]
		{
			new(promotionId,
				new DateTime(2022, 7, 25, 16, 0, 0, DateTimeKind.Utc),
				new DateTime(2022, 7, 31, 16, 0, 0, DateTimeKind.Utc),
				brandId,
				"{}")
		};

		var depositForms = new DepositFormInfo[]
		{
			new(Guid.NewGuid(),
				"Player1",
				brandId,
				promotionId,
				1000,
				1000),
			new(Guid.NewGuid(),
				"Player2",
				brandId,
				promotionId,
				1200,
				1300),
		};

		var command = new DistributeDepositAccumulateRouletteCountCommand(DateOnly.Parse("2022-07-29"));

		_ = fakeBackgroundRepository
			.GetValidDepositAccumulateRoulettePromotionsAsync(
				Arg.Is(command.ProcessDate),
				Arg.Any<CancellationToken>())
			.Returns(promotions.ToAsyncEnumerable());

		_ = fakeBackgroundRepository.GetApprovedDepositFormsAsync(
				Arg.Is<IEnumerable<Guid>>(x => x.Contains(brandId)),
				Arg.Is(command.ProcessDate),
				Arg.Any<CancellationToken>())
			.Returns(depositForms.ToAsyncEnumerable());

		var sut = new DistributeDepositAccumulateRouletteCountCommandService(fakeBackgroundRepository, serializer);

		var actual = await sut.QueryAsync(command).ToArrayAsync();

		Assert.AreEqual(0, actual.Length);

		_ = fakeBackgroundRepository
			.DidNotReceive()
			.AddDepositAccumulateRouletteEventsAsync(
				Arg.Is(command.ProcessDate),
				Arg.Any<IEnumerable<DepositAccumulateRouletteEventAddInfo>>(),
				Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task Not_Store_EventData_By_Daily_When_Player_Not_Reach_Deposit_Amount()
	{
		var fakeBackgroundRepository = Substitute.For<IBackgroundRepository>();
		var serializer = new DepositAccumulateRouletteSerializer();

		var settings = new DepositAccumulateRouletteSettings(
			new DepositAccumulateItem[]
			{
				new(Guid.Parse("ab85fad0-fa30-46ef-9963-693fdbe8deb5"), 1000M, 1)
			},
			new RouletteItem[]
			{
				new(Guid.Parse("1cd30c0a-a61c-49fc-ba95-0ba78bfbcbb1"), "iphone 13 pro", 3M, 0M, 0M)
			});

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();

		var promotions = new PromotionInfo[]
		{
			new(promotionId,
				new DateTime(2022, 7, 25, 16, 0, 0, DateTimeKind.Utc),
				new DateTime(2022, 7, 31, 16, 0, 0, DateTimeKind.Utc),
				brandId,
				serializer.Serialize(settings))
		};

		var depositForms = new DepositFormInfo[]
		{
			new(Guid.NewGuid(),
				"Player1",
				brandId,
				promotionId,
				null,
				500)
		};

		var command = new DistributeDepositAccumulateRouletteCountCommand(DateOnly.Parse("2022-07-29"));

		_ = fakeBackgroundRepository
			.GetValidDepositAccumulateRoulettePromotionsAsync(Arg.Is(command.ProcessDate), Arg.Any<CancellationToken>())
			.Returns(promotions.ToAsyncEnumerable());

		_ = fakeBackgroundRepository.GetApprovedDepositFormsAsync(
				Arg.Is<IEnumerable<Guid>>(x => x.Contains(brandId)),
				Arg.Is(command.ProcessDate),
				Arg.Any<CancellationToken>())
			.Returns(depositForms.ToAsyncEnumerable());

		var sut = new DistributeDepositAccumulateRouletteCountCommandService(fakeBackgroundRepository, serializer);

		var actual = await sut.QueryAsync(command).ToArrayAsync();

		Assert.AreEqual(0, actual.Length);

		_ = fakeBackgroundRepository
			.DidNotReceive()
			.AddDepositAccumulateRouletteEventsAsync(
				Arg.Is(command.ProcessDate),
				Arg.Any<IEnumerable<DepositAccumulateRouletteEventAddInfo>>(),
				Arg.Any<CancellationToken>());
	}
}
