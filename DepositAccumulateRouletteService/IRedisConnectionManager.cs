using StackExchange.Redis;

namespace Tgs.Promotion.DepositAccumulateRouletteService;

public interface IRedisConnectionManager
{
	IDatabase Database { get; }
}
