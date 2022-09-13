using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Promotion.DepositAccumulateRoulette.Common;

internal class RouletteItemJsonConverter : JsonConverter<RouletteItem>
{
	private readonly JsonSerializerOptions _jsonSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};

	public override RouletteItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var item = JsonSerializer.Deserialize<RouletteItem>(ref reader, _jsonSerializerOptions);

		return item is not null && item.Id == Guid.Empty
			? item with { Id = Guid.NewGuid() }
			: item;
	}

	public override void Write(Utf8JsonWriter writer, RouletteItem value, JsonSerializerOptions options)
		=> JsonSerializer.Serialize(writer, value, _jsonSerializerOptions);
}
