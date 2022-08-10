using Google.Protobuf;
using NSubstitute;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Core;
using Tgs.Promotion.DepositAccumulateRoulette.Core.ApplicationServices;
using Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;
using Tgs.ServiceUtility;

using Common = Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRouletteService.UnitTests;

internal class RouletteSettingQueryProcessorTests
{
	[Test]
	public async Task Query_Request_Will_Response()
	{
		var fakeGetService = Substitute.For<IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>>();

		var request = new RouletteSettingQueryRequest
		{
			PromotionId = Guid.NewGuid(),
		};

		var promotion = new DepositAccumulateRoulettePromotion(
			request.PromotionId,
			null,
			new DepositAccumulateRouletteSettings(
				new DepositAccumulateItem[]
				{
					new(Guid.Parse("4398bd5f-cbd9-4982-a145-c98ceb4c5ea7"), 1000M, 1),
					new(Guid.Parse("c6149bd1-ac94-4e83-897e-434e1ea0fffa"), 2800M, 3)
				},
				new Common.RouletteItem[]
				{
					new(Guid.Parse("8eb0f681-fe27-478f-9c1d-bdfdad8d3dfc"), "iphone 13 pro", 3M, 0M, 0M),
					new(Guid.Parse("3b776382-a909-4c4c-b739-4d7974eebb47"), "1000PHP", 3M, 0.25M, 1000M),
					new(Guid.Parse("aadd13cf-e1ea-4a53-9426-46f44cb7dbd4"), "300PHP", 3M, 0.75M, 300M)
				}));

		var expected = new[]
		{
			new Protos.RouletteItem
			{
				Id =Guid.Parse("8eb0f681-fe27-478f-9c1d-bdfdad8d3dfc"),
				Title = "iphone 13 pro"
			},
			new Protos.RouletteItem
			{
				Id =Guid.Parse("3b776382-a909-4c4c-b739-4d7974eebb47"),
				Title = "1000PHP"
			},
			new Protos.RouletteItem
			{
				Id =Guid.Parse("aadd13cf-e1ea-4a53-9426-46f44cb7dbd4"),
				Title = "300PHP"
			}
		};

		_ = fakeGetService.GetAsync(
					Arg.Is(new GetDepositAccumulateRoulettePromotionQuery(request.PromotionId)),
					Arg.Any<CancellationToken>())
				.Returns(promotion);

		var sut = new RouletteSettingQueryProcessor(fakeGetService);

		var response = await sut.HandleAsync(request.ToByteArray());

		var actual = RouletteSettingQueryResponse.Parser.ParseFrom(response.Span);

		Assert.IsNotNull(actual);
		Assert.AreEqual(request.PromotionId, actual.PromotionId);
		CollectionAssert.AreEqual(expected, actual.Items);
	}

	[Test]
	public async Task Query_Request_Will_Return_Empty_When_Promotion_Is_Not_Found()
	{
		var fakeGetService = Substitute.For<IGetService<GetDepositAccumulateRoulettePromotionQuery, DepositAccumulateRoulettePromotion?>>();

		var request = new RouletteSettingQueryRequest
		{
			PromotionId = Guid.NewGuid(),
		};

		_ = fakeGetService.GetAsync(
					Arg.Is(new GetDepositAccumulateRoulettePromotionQuery(request.PromotionId)),
					Arg.Any<CancellationToken>())
				.Returns((DepositAccumulateRoulettePromotion?)null);

		var sut = new RouletteSettingQueryProcessor(fakeGetService);

		var response = await sut.HandleAsync(request.ToByteArray());

		var actual = RouletteSettingQueryResponse.Parser.ParseFrom(response.Span);

		Assert.IsNotNull(actual);
		Assert.AreEqual(null, actual.PromotionId);
		Assert.AreEqual(0, actual.Items.Count);
	}
}
