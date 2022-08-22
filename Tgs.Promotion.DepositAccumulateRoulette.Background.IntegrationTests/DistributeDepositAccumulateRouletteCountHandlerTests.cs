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
		var fakeCommandService = Substitute.For<ICommandService<DistributeDepositAccumulateRouletteCountCommand>>();

		var sut = new DistributeDepositAccumulateRouletteCountHandler(fakeCommandService);

		var request = new ProcessRequest
		{
			ProcessDate = DateOnly.Parse("2022-07-01")
		};

		await sut.HandleAsync(request.ToByteArray(), CancellationToken.None);

		_ = fakeCommandService.Received(1)
			.ExecuteAsync(Arg.Is<DistributeDepositAccumulateRouletteCountCommand>(x => x.ProcessDate == (DateOnly)request.ProcessDate));
	}
}
