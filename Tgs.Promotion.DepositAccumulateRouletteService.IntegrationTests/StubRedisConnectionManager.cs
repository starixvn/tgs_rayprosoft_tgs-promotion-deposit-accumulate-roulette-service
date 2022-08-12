using StackExchange.Redis;

namespace Tgs.Promotion.DepositAccumulateRouletteService.IntegrationTests;

internal class StubRedisConnectionManager : IRedisConnectionManager
{
	public IDatabase Database { get; }

	public StubRedisConnectionManager(IDatabase database)
	{
		Database = database;
	}
}
