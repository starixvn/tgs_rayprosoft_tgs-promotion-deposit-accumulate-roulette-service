using NSubstitute;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class RouletteRecordsQueryTests
{
	[Test]
	public async Task Query_Roulette_Record_List_By_PlayerCode_And_PromotionId()
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
	public async Task Query_Roulette_Record_List()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var query = new RouletteRecordsConditionQuery(
			Guid.NewGuid(),
			["player1", "player2"],
			[Guid.NewGuid()],
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

	[Test]
	public async Task Get_Roulette_Record_Info()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var query = new GetRouletteRecordQuery(
			Guid.NewGuid(),
			Guid.NewGuid());

		var sut = new GetRouletteRecordQueryService(fakeTgsRepository);

		_ = await sut.GetAsync(query);

		_ = fakeTgsRepository
			.Received(1)
			.GetRouletteRecordInfoAsync(
				Arg.Is(query.RecordId));
	}
}
