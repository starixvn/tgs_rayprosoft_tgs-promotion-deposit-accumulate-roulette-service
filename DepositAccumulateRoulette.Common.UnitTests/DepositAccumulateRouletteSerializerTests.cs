using NUnit.Framework.Legacy;

namespace Tgs.Promotion.DepositAccumulateRoulette.Common.UnitTests;

internal class DepositAccumulateRouletteSerializerTests
{
	[Test]
	public void Can_Deserialize_Rule()
	{
		var settings = @"
{
  ""depositItems"": [
	{
	  ""id"": ""ab85fad0-fa30-46ef-9963-693fdbe8deb5"",
	  ""rouletteCount"": 1,
	  ""accumulateAmount"": 1000
	}
  ],
  ""rouletteItems"": [
	{
	  ""id"": ""1cd30c0a-a61c-49fc-ba95-0ba78bfbcbb1"",
	  ""bonus"": 0,
	  ""title"": ""iphone 13 pro"",
	  ""turnoverRate"": 3,
	  ""winningProbability"": 0
	}
  ]
}";
		var expected = new DepositAccumulateRouletteSettings(
			new DepositAccumulateItem[]
			{
				new(Guid.Parse("ab85fad0-fa30-46ef-9963-693fdbe8deb5"), 1000M, 1)
			},
			new RouletteItem[]
			{
				new(Guid.Parse("1cd30c0a-a61c-49fc-ba95-0ba78bfbcbb1"), "iphone 13 pro", 3M, 0M, 0M)
			});

		var sut = new DepositAccumulateRouletteSerializer();

		var actual = sut.Deserialize<DepositAccumulateRouletteSettings>(settings);

		ClassicAssert.IsNotNull(actual);
		CollectionAssert.AreEqual(expected.DepositItems, actual!.DepositItems);
		CollectionAssert.AreEqual(expected.RouletteItems, actual!.RouletteItems);
	}

	[Test]
	public void Can_Serialize_Rule()
	{
		var settings = new DepositAccumulateRouletteSettings(
			new DepositAccumulateItem[]
			{
				new(Guid.Parse("ab85fad0-fa30-46ef-9963-693fdbe8deb5"), 100M, 1)
			},
			new RouletteItem[]
			{
				new(Guid.Parse("1cd30c0a-a61c-49fc-ba95-0ba78bfbcbb1"), "iphone 13 pro", 3M, 0M, 0M)
			});

		var sut = new DepositAccumulateRouletteSerializer();

		var actual = sut.Serialize(settings);

		ClassicAssert.IsNotNull(actual);

		var deserialized = sut.Deserialize<DepositAccumulateRouletteSettings>(actual);

		CollectionAssert.AreEqual(settings.DepositItems, deserialized!.DepositItems);
		CollectionAssert.AreEqual(settings.RouletteItems, deserialized!.RouletteItems);
	}
}
