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
}
