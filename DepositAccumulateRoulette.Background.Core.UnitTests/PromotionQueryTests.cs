using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.Promotion.DepositAccumulateRoulette.Background.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.Core.UnitTests;

internal class PromotionQueryTests
{
	[Test]
	public async Task 查詢有效的活動_查詢日期在活動期間內()
	{
		var fakePromotionClient = Substitute.For<IPromotionServiceClient>();

		var sut = new ValidPromotionListQueryService(fakePromotionClient);

		var processDate = DateOnly.Parse("2023-04-11");

		var promotions = new[]
		{
			new PromotionMainInfo
			{
				Id = Guid.NewGuid(),
				BrandId = Guid.NewGuid(),
				PromotionType = "DepositAccumulateRoulette",
				Name = "TestPromotion1",
				EventDuring = new During(
					From: DateTime.Parse("2023-04-05 16:00:00"),
					To: DateTime.Parse("2023-04-15 16:00:00"))
			}
		};

		_ = fakePromotionClient.ListPromotionsByPromotionTypeAsync(
				Arg.Is("DepositAccumulateRoulette"),
				Arg.Any<CancellationToken>())
			.Returns(promotions.ToAsyncEnumerable());

		_ = fakePromotionClient.GetPromotionRuleAsync<ValidPromotionListQueryService.PromotionRule>(
				Arg.Any<Guid>(),
				Arg.Any<CancellationToken>())
			.Returns(new ValidPromotionListQueryService.PromotionRule(
				new DepositAccumulateRouletteSettings(
					new DepositAccumulateItem[]
					{
						new(Guid.NewGuid(), 1000M, 1)
					},
					new RouletteItem[]
					{
						new(Guid.NewGuid(), "iphone 13 pro", 3M, 0M, 0M)
					})));

			var query = new ValidPromotionListQuery(processDate);

		var actual = await sut.QueryAsync(query).ToArrayAsync();

		Assert.AreEqual(1, actual.Length);
	}

	[Test]
	public async Task 查詢有效的活動_查詢日期在活動期間的隔天()
	{
		var fakePromotionClient = Substitute.For<IPromotionServiceClient>();

		var sut = new ValidPromotionListQueryService(fakePromotionClient);

		var processDate = DateOnly.Parse("2023-04-11");

		var promotions = new[]
		{
			new PromotionMainInfo
			{
				Id = Guid.NewGuid(),
				BrandId = Guid.NewGuid(),
				PromotionType = "DepositAccumulateRoulette",
				Name = "TestPromotion1",
				EventDuring = new During(
					From: DateTime.Parse("2023-04-05 16:00:00"),
					To: DateTime.Parse("2023-04-10 16:00:00"))
			}
		};

		_ = fakePromotionClient.ListPromotionsByPromotionTypeAsync(
				Arg.Is("DepositAccumulateRoulette"),
				Arg.Any<CancellationToken>())
			.Returns(promotions.ToAsyncEnumerable());

		_ = fakePromotionClient.GetPromotionRuleAsync<ValidPromotionListQueryService.PromotionRule>(
				Arg.Any<Guid>(),
				Arg.Any<CancellationToken>())
			.Returns(new ValidPromotionListQueryService.PromotionRule(
				new DepositAccumulateRouletteSettings(
					new DepositAccumulateItem[]
					{
						new(Guid.NewGuid(), 1000M, 1)
					},
					new RouletteItem[]
					{
						new(Guid.NewGuid(), "iphone 13 pro", 3M, 0M, 0M)
					})));

			var query = new ValidPromotionListQuery(processDate);

		var actual = await sut.QueryAsync(query).ToArrayAsync();

		Assert.AreEqual(1, actual.Length);
	}

	[Test]
	public async Task 查詢有效的活動_不回傳沒有沒有PromotionRule的活動()
	{
		var fakePromotionClient = Substitute.For<IPromotionServiceClient>();

		var sut = new ValidPromotionListQueryService(fakePromotionClient);

		var processDate = DateOnly.Parse("2023-04-11");

		var promotions = new[]
		{
			new PromotionMainInfo
			{
				Id = Guid.NewGuid(),
				BrandId = Guid.NewGuid(),
				PromotionType = "DepositAccumulateRoulette",
				Name = "TestPromotion1",
				EventDuring = new During(
					From: DateTime.Parse("2023-04-05 16:00:00"),
					To: DateTime.Parse("2023-04-15 16:00:00"))
			}
		};

		_ = fakePromotionClient.ListPromotionsByPromotionTypeAsync(
				Arg.Is("DepositAccumulateRoulette"),
				Arg.Any<CancellationToken>())
			.Returns(promotions.ToAsyncEnumerable());

		_ = fakePromotionClient.GetPromotionRuleAsync<ValidPromotionListQueryService.PromotionRule>(
				Arg.Any<Guid>(),
				Arg.Any<CancellationToken>())
			.Returns((ValidPromotionListQueryService.PromotionRule?) null);

		var query = new ValidPromotionListQuery(processDate);

		var actual = await sut.QueryAsync(query).ToArrayAsync();

		Assert.AreEqual(0, actual.Length);
	}
}
