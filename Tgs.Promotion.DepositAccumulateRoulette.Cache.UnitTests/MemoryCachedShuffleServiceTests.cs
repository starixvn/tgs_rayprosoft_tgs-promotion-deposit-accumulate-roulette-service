using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;
using Tgs.Promotion.DepositAccumulateRoulette.Common;
using Tgs.Promotion.DepositAccumulateRoulette.Core;

namespace Tgs.Promotion.DepositAccumulateRoulette.Cache.UnitTests;

internal class MemoryCachedShuffleServiceTests
{
	[Test]
	public async Task Get_Shuffled_Deck_When_Hit_Cache()
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

		var fakeMemoryCache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions()));
		var fakeShuffleService = Substitute.For<IShuffleService>();

		var promotionId = Guid.NewGuid();

		var expected = await new ShuffleService().ShuffleAsync(promotionId, rouletteItems).ConfigureAwait(false);

		_ = fakeMemoryCache.Set($"Deck:{promotionId}", expected);

		var sut = new MemoryCachedShuffleService(fakeMemoryCache, fakeShuffleService);

		var actual = await sut.ShuffleAsync(promotionId, rouletteItems).ConfigureAwait(false);

		CollectionAssert.AreEqual(expected, actual);

		_ = fakeShuffleService
			.DidNotReceive()
			.ShuffleAsync(Arg.Is(promotionId), Arg.Is(rouletteItems));
	}

	[Test]
	public async Task Get_Shuffled_Deck_When_Not_Hit_Cache()
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

		var fakeMemoryCache = new MemoryCache(new OptionsWrapper<MemoryCacheOptions>(new MemoryCacheOptions()));
		var fakeShuffleService = Substitute.For<IShuffleService>();

		var promotionId = Guid.NewGuid();

		var expected = await new ShuffleService().ShuffleAsync(promotionId, rouletteItems).ConfigureAwait(false);

		_ = fakeShuffleService
			.ShuffleAsync(Arg.Is(promotionId), Arg.Is(rouletteItems))
			.Returns(expected);

		var sut = new MemoryCachedShuffleService(fakeMemoryCache, fakeShuffleService);

		var actual = await sut.ShuffleAsync(promotionId, rouletteItems).ConfigureAwait(false);

		CollectionAssert.AreEqual(expected, actual);

		_ = fakeShuffleService
			.Received(1)
			.ShuffleAsync(Arg.Is(promotionId), Arg.Is(rouletteItems));
	}
}
