using System.Text.Json.Serialization;
using System.Text.Json;
using DateOnlyTimeOnly.AspNet.Converters;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace Tgs.DepositAccumulateRoulette.Clients;

internal class DepositAccumulateRouletteServiceClient : IDepositAccumulateRouletteServiceClient
{
	private static readonly JsonSerializerOptions _SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
		Converters =
		{
			new JsonStringEnumConverter(),
			new DateOnlyJsonConverter()
		}
	};

	private readonly HttpClient _httpClient;

	public DepositAccumulateRouletteServiceClient(
		HttpClient httpClient)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	}

	public async IAsyncEnumerable<RouletteRecord> GetRouletteRecordsAsync(
		Guid promotionId,
		string playerCode,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var response = await _httpClient
			.GetAsync($"/api/roulette/records/{playerCode}/{promotionId}", cancellationToken)
			.ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		var rouletteRecords = await response.Content
			.ReadFromJsonAsync<IEnumerable<RouletteRecord>>(_SerializerOptions, cancellationToken)
			.ConfigureAwait(false);

		rouletteRecords ??= Array.Empty<RouletteRecord>();

		foreach (var info in rouletteRecords)
			yield return info;
	}
}
