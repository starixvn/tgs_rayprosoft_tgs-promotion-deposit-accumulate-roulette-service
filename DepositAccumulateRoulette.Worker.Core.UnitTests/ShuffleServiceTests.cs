using NUnit.Framework.Legacy;
using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class ShuffleServiceTests
{
	[Test]
	public async Task Shuffle_By_RouletteItems()
	{
		var rouletteItems = new RouletteItem[]
		{
			new(Guid.NewGuid(), "iphone", 3, 0M, 0M),
			new(Guid.NewGuid(),"100000PHP", 3, 0M,0M),
			new(Guid.NewGuid(),"30000PHP", 3,0M, 0M),
			new(Guid.NewGuid(),"8888PHP", 3,0M,0M),
			new(Guid.NewGuid(),"1800PHP", 3,0.02M, 1800M),
			new(Guid.NewGuid(),"800PHP", 3,0.03M, 800M),
			new(Guid.NewGuid(),"300PHP", 3,0.04M, 300M),
			new(Guid.NewGuid(),"120PHP",3, 0.06M, 120M),
			new(Guid.NewGuid(),"50PHP", 3,0.25M, 50M),
			new(Guid.NewGuid(),"30PHP", 3,0.20M, 30M),
			new(Guid.NewGuid(),"18PHP", 3,0.30M, 18M),
			new(Guid.NewGuid(),"Thanks",3, 0.10M, 0M)
		};

		var sut = new ShuffleService();

		var actual = await sut.ShuffleAsync(Guid.NewGuid(), rouletteItems).ConfigureAwait(false);

		ClassicAssert.AreEqual(10000, actual.Length);

		ClassicAssert.AreEqual(0, actual.Count(item => item == 0));
		ClassicAssert.AreEqual(0, actual.Count(item => item == 1));
		ClassicAssert.AreEqual(0, actual.Count(item => item == 2));
		ClassicAssert.AreEqual(0, actual.Count(item => item == 3));
		ClassicAssert.AreEqual(200, actual.Count(item => item == 4));
		ClassicAssert.AreEqual(300, actual.Count(item => item == 5));
		ClassicAssert.AreEqual(400, actual.Count(item => item == 6));
		ClassicAssert.AreEqual(600, actual.Count(item => item == 7));
		ClassicAssert.AreEqual(2500, actual.Count(item => item == 8));
		ClassicAssert.AreEqual(2000, actual.Count(item => item == 9));
		ClassicAssert.AreEqual(3000, actual.Count(item => item == 10));
		ClassicAssert.AreEqual(1000, actual.Count(item => item == 11));
	}
}
