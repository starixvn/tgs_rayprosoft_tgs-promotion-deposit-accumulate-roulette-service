using StackExchange.Redis;

namespace Tgs.Promotion.DepositAccumulateRoulette.Core;

public class RedisConnectionManager : IDisposable
{
	private readonly ConnectionMultiplexer _connectionMultiplexer;
	private bool _disposedValue;

	public IDatabase Database { get; }

	public RedisConnectionManager(string redisConnectionString)
	{
		_connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);

		Database = _connectionMultiplexer.GetDatabase();
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposedValue)
		{
			if (disposing)
			{
				_connectionMultiplexer.Close();
				_connectionMultiplexer.Dispose();
			}

			_disposedValue = true;
		}
	}
}
