using System.Linq;
using NSubstitute;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class RouletteRecordsQueryTests
{
	[Test]
	public async Task Query_Roulette_Records_With_PlayerCode_And_PromotionId()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var query = new RouletteRecordsQuery(Guid.NewGuid(), "player1");

		var sut = new RouletteRecordsQueryService(fakeTgsRepository);

		_ = await sut.QueryAsync(query).ToArrayAsync();

		_ = fakeTgsRepository
			.Received(1)
			.QueryRouletteRecordListAsync(
				Arg.Is(query.PromotionId),
				Arg.Is(query.PlayerCode));
	}

	[Test]
	public async Task QueryRoulette_Records_With_Conditions()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var query = new RouletteRecordsConditionQuery(
			Guid.NewGuid(),
			new[] { "player1", "player2" },
			new[] { Guid.NewGuid() },
			DateTimeOffset.Parse("2022-11-10 00:00:00+08:00"),
			DateTimeOffset.Parse("2022-11-17 00:00:00+08:00"));

		var sut = new RouletteRecordsConditionQueryService(fakeTgsRepository);

		_ = await sut.QueryAsync(query).ToArrayAsync();

		_ = fakeTgsRepository
			.Received(1)
			.QueryRouletteRecordListAsync(
				Arg.Is(query.BrandId),
				Arg.Is<IEnumerable<string>>(players => players.SequenceEqual(query.PlayerCodes)),
				Arg.Is<IEnumerable<Guid>>(promotionIds => promotionIds.SequenceEqual(query.PromotionIds)),
				Arg.Is(query.BeginTime.UtcDateTime),
				Arg.Is(query.EndTime.UtcDateTime),
				Arg.Any<CancellationToken>());
	}
}
