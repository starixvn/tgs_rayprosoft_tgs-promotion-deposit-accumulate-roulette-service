using Tgs.Promotion.DepositAccumulateRoulette.Common;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

internal class ShuffleService : IShuffleService
{
	private static readonly Random _Random = new();

	public ValueTask<int[]> ShuffleAsync(Guid promotionId, IEnumerable<RouletteItem> items)
	{
		var proportions = items
			.Select((item, index) => new
			{
				ItemIndex = index,
				WinningProbability = Math.Floor(item.WinningProbability * 10000M)
			}).ToArray();

		var size = (int)proportions.Sum(x => x.WinningProbability);

		var deck = proportions.Aggregate(new List<int>(size), (seed, t) =>
		{
			seed.AddRange(Enumerable.Repeat(t.ItemIndex, (int)t.WinningProbability));
			return seed;
		});

		for (var i = 0; i < 2; i++)
			deck = Shuffle(deck);

		return ValueTask.FromResult(deck.ToArray());
	}

	private static List<int> Shuffle(IEnumerable<int> deck)
	{
		var newDeck = deck.ToList();

		for (var i = 0; i < newDeck.Count; i++)
		{
			var index = _Random.Next(0, newDeck.Count - 1);
			if (index == i)
				continue;

			(newDeck[i], newDeck[index]) = (newDeck[index], newDeck[i]);
		}

		return newDeck;
	}
}
