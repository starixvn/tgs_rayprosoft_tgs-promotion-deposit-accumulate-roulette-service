using Google.Protobuf;
using NSubstitute;
using Tgs.Promotion.DepositAccumulateRoulette.Background.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRoulette.Background.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Background.Protos;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Background.IntegrationTests;

internal class DistributeDepositAccumulateRouletteCountHandlerTests
{
	[Test]
	public async Task Process_Request_By_Daily()
	{
		var fakeDistributeCommandService = Substitute.For<IQueryService<DistributeDepositAccumulateRouletteCountCommand, DepositAccumulateRouletteEventAddInfo>>();
		var fakeClearCacheService = Substitute.For<ICommandService<ClearExpiredEventCacheCommand>>();

		var sut = new DistributeDepositAccumulateRouletteCountHandler(fakeDistributeCommandService, fakeClearCacheService);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-07-01")
		};

		await sut.HandleAsync("test", request.ToByteArray(), null);

		_ = fakeDistributeCommandService.Received(1)
			.QueryAsync(Arg.Is<DistributeDepositAccumulateRouletteCountCommand>(x => x.ProcessDate == (DateOnly)request.ProcessDate));
	}
}
