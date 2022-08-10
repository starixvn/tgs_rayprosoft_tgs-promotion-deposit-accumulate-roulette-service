namespace Tgs.Promotion.DepositAccumulateRoulette.Core.UnitTests;

internal class DistributePlanHasherTests
{
	[Test]
	public void Get_Hash_Code()
	{
		var eventId = Guid.Parse("89e984d8-bb60-48bb-bc5b-ad8ec8b46eb0");
		var count = 10;

		var expected = 1058084024;

		var sut = new DistributePlanHasher();

		var actual = sut.GetHashCode(eventId, count);

		Assert.AreEqual(expected, actual);
	}
}
