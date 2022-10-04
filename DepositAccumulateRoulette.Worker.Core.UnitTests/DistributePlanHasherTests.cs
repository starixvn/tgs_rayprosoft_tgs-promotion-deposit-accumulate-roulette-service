namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Core.UnitTests;

internal class DistributePlanHasherTests
{
	[Test]
	public void Get_Hash_Code()
	{
		var date = new DateOnly(2022, 08, 30);
		var count = 10;

		var expected = 1508689012;

		var sut = new DistributePlanHasher();

		var actual = sut.GetHashCode(date, count);

		Assert.AreEqual(expected, actual);
	}

	[Test]
	public void Hash_Code_Duplicate()
	{
		var item1 = new
		{
			Date = new DateOnly(2022, 12, 16),
			RouletteCount = 79
		};

		var item2 = new
		{
			Date = new DateOnly(2026, 7, 16),
			RouletteCount = 38
		};

		var sut = new DistributePlanHasher();

		var hash1 = sut.GetHashCode(item1.Date, item1.RouletteCount);
		var hash2 = sut.GetHashCode(item2.Date, item2.RouletteCount);

		Assert.AreEqual(hash1, hash2);
	}
}
