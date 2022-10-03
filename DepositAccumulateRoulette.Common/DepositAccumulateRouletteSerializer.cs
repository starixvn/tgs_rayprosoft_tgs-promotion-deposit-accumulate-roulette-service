using System.Text.Json;

namespace Tgs.Promotion.DepositAccumulateRoulette.Common;

public class DepositAccumulateRouletteSerializer
{
	private readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		Converters =
		{
			new DepositAccumulateItemJsonConverter(),
			new RouletteItemJsonConverter()
		}
	};

	public T? Deserialize<T>(string json)
		=> JsonSerializer.Deserialize<T>(json, _jsonSerializerOptions);

	public string Serialize<T>(T data)
		=> JsonSerializer.Serialize(data, _jsonSerializerOptions);
}
