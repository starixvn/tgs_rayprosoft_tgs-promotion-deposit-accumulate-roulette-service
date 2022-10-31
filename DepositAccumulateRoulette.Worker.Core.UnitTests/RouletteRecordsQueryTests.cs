using NSubstitute;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class RouletteRecordsQueryTests
{
	[Test]
	public async Task Get_Roulette_RecordsAsync()
	{
		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		var query = new GetRouletteRecordsQuery(Guid.NewGuid(), "player1");

		var sut = new GetRouletteRecordsQueryService(fakeTgsRepository);

		_ = await sut.QueryAsync(query).ToArrayAsync();

		_ = fakeTgsRepository
			.Received(1)
			.GetRouletteRecordInfosAsync(
				Arg.Is(query.PromotionId),
				Arg.Is(query.PlayerCode));
	}
}
