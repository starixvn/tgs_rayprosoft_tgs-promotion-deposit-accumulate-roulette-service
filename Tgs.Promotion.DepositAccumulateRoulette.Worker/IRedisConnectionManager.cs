using StackExchange.Redis;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker;

public interface IRedisConnectionManager
{
	IDatabase Database { get; }
}
