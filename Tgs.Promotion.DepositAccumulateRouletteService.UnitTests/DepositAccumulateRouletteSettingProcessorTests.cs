using ExpectedObjects;
using Google.Protobuf;
using Tgs.Promotion.Clients.Protos;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRouletteService.MessageQueue;

namespace Tgs.Promotion.DepositAccumulateRouletteService.UnitTests;

public class DepositAccumulateRouletteSettingProcessorTests
{
    [Test]
    public async Task Setting_Request_Will_Response()
    {
        var depositItems = new DepositAccumulateItem[]
        {
            new(Guid.NewGuid(), 1000M, 1),
            new(Guid.NewGuid(), 2800M, 3)
        };

        var rouletteItems = new RouletteItem[]
        {
            new(Guid.NewGuid(), "iphone 13 pro", 3M, 0M, 0M),
            new(Guid.NewGuid(), "1000PHP", 3M, 0.25M, 1000M),
            new(Guid.NewGuid(),"300PHP", 3M, 0.75M, 300M)
        };

        var settings = new DepositAccumulateRouletteSettings(depositItems, rouletteItems);

        var serializer = new DepositAccumulateRouletteSerializer();

        var request = new CampaignDetailRequest
        {
            PromotionId = Guid.NewGuid(),
            Settings = serializer.Serialize(settings)
        };

        var sut = new DepositAccumulateRouletteSettingProcessor(serializer);

        var expectedSettings = new DepositAccumulateRouletteSettings(
            new[]
            {
                depositItems[0],
                depositItems[1]
            },
            new RouletteItem[]
            {
                rouletteItems[0],
                rouletteItems[1],
                rouletteItems[2]
            });

        var response = await sut.HandleAsync(request.ToByteArray());

        var actual = CampaignDetailResponse.Parser.ParseFrom(response.Span);

        var actualSettings = serializer.Deserialize<DepositAccumulateRouletteSettings>(actual.CampaignDetail);

        Assert.IsNotNull(actualSettings);

        CollectionAssert.AreEqual(expectedSettings.DepositItems, actualSettings!.DepositItems);
        CollectionAssert.AreEqual(expectedSettings.RouletteItems, actualSettings.RouletteItems);
        Assert.AreEqual(1M, actualSettings.RouletteItems.Sum(item => item.WinningProbability));
    }

    [Test]
    public void Must_Have_At_Least_One_DepositItem()
    {
        var rouletteItems = new RouletteItem[]
        {
            new(Guid.NewGuid(), "iphone 13 pro", 3M, 0M, 0M),
            new(Guid.NewGuid(), "1000PHP", 3M, 0.25M, 1000M),
            new(Guid.NewGuid(),"300PHP", 3M, 0.75M, 300M)
        };

        var settings = new DepositAccumulateRouletteSettings(Array.Empty<DepositAccumulateItem>(), rouletteItems);

        var serializer = new DepositAccumulateRouletteSerializer();

        var request = new CampaignDetailRequest
        {
            PromotionId = Guid.NewGuid(),
            Settings = serializer.Serialize(settings)
        };

        var sut = new DepositAccumulateRouletteSettingProcessor(serializer);

        Assert.ThrowsAsync<InvalidOperationException>(async ()
            => await sut.HandleAsync(request.ToByteArray()));
    }

    [Test]
    public void Must_Have_At_Least_One_RouletteItem()
    {
        var depositItems = new DepositAccumulateItem[]
        {
            new(Guid.NewGuid(), 1000M, 1),
            new(Guid.NewGuid(), 2800M, 3)
        };

        var settings = new DepositAccumulateRouletteSettings(depositItems, Array.Empty<RouletteItem>());

        var serializer = new DepositAccumulateRouletteSerializer();

        var request = new CampaignDetailRequest
        {
            PromotionId = Guid.NewGuid(),
            Settings = serializer.Serialize(settings)
        };

        var sut = new DepositAccumulateRouletteSettingProcessor(serializer);

        Assert.ThrowsAsync<InvalidOperationException>(async ()
            => await sut.HandleAsync(request.ToByteArray()));
    }

    [Test]
    public void All_WinningProbabilities_Of_RouletteItems_Should_Equals_One()
    {
        var depositItems = new DepositAccumulateItem[]
        {
            new(Guid.NewGuid(), 1000M, 1),
            new(Guid.NewGuid(), 2800M, 3)
        };

        var rouletteItems = new RouletteItem[]
        {
            new(Guid.NewGuid(), "iphone 13 pro", 3M, 0M, 0M),
            new(Guid.NewGuid(), "1000PHP", 3M, 0.25M, 1000M),
            new(Guid.NewGuid(),"300PHP", 3M, 0.65M, 300M)
        };

        var settings = new DepositAccumulateRouletteSettings(depositItems, rouletteItems);

        var serializer = new DepositAccumulateRouletteSerializer();

        var request = new CampaignDetailRequest
        {
            PromotionId = Guid.NewGuid(),
            Settings = serializer.Serialize(settings)
        };

        var sut = new DepositAccumulateRouletteSettingProcessor(serializer);

        Assert.ThrowsAsync<InvalidOperationException>(async ()
            => await sut.HandleAsync(request.ToByteArray()));
    }

    [Test]
    public async Task Create_New_Item_Will_Auto_Assign_Id()
    {
        var request = new CampaignDetailRequest
        {
            PromotionId = Guid.NewGuid(),
            Settings = "{\"depositItems\":[{\"rouletteCount\":1,\"accumulateAmount\":1000},{\"rouletteCount\":3,\"accumulateAmount\":2800}],\"rouletteItems\":[{\"bonus\":0,\"title\":\"iphone 13 pro\",\"turnoverRate\":3,\"winningProbability\":0},{\"bonus\":1000,\"title\":\"1000PHP\",\"turnoverRate\":3,\"winningProbability\":0.25},{\"bonus\":300,\"title\":\"300PHP\",\"turnoverRate\":3,\"winningProbability\":0.75}]}"
        };

        var serializer = new DepositAccumulateRouletteSerializer();

        var sut = new DepositAccumulateRouletteSettingProcessor(serializer);

        var depositItems = new DepositAccumulateItem[]
        {
            new(Guid.NewGuid(), 1000M, 1),
            new(Guid.NewGuid(), 2800M, 3)
        };

        var rouletteItems = new RouletteItem[]
        {
            new(Guid.NewGuid(), "iphone 13 pro", 3M, 0M, 0M),
            new(Guid.NewGuid(), "1000PHP", 3M, 0.25M, 1000M),
            new(Guid.NewGuid(),"300PHP", 3M, 0.75M, 300M)
        };

        var expectedSettings = new DepositAccumulateRouletteSettings(depositItems, rouletteItems);

        var response = await sut.HandleAsync(request.ToByteArray());

        var actual = CampaignDetailResponse.Parser.ParseFrom(response.Span);

        var actualSettings = serializer.Deserialize<DepositAccumulateRouletteSettings>(actual.CampaignDetail);

        Assert.IsNotNull(actualSettings);

        var actualDepositItems = actualSettings!.DepositItems.ToList();

        actualDepositItems.ForEach(item =>
        {
            var idx = actualDepositItems.IndexOf(item);

            Assert.AreNotEqual(Guid.Empty, item.Id);

            expectedSettings.DepositItems.ElementAt(idx).ToExpectedObject(
                ctx => ctx
                    .Ignore(x => x.Id)
                    .IgnoreEqualsOverride())
                .ShouldEqual(item);
        });

        var actualRouletteItems = actualSettings.RouletteItems.ToList();

        actualRouletteItems.ForEach(item =>
        {
            var idx = actualRouletteItems.IndexOf(item);

            Assert.AreNotEqual(Guid.Empty, item.Id);

            expectedSettings.RouletteItems.ElementAt(idx).ToExpectedObject(
                    ctx => ctx
                        .Ignore(x => x.Id)
                        .IgnoreEqualsOverride())
                .ShouldEqual(item);
        });

        Assert.AreEqual(1M, actualSettings!.RouletteItems.Sum(item => item.WinningProbability));
    }
}