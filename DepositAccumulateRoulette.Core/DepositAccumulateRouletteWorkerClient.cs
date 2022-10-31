using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json;
using DateOnlyTimeOnly.AspNet.Converters;

namespace Tgs.Promotion.DepositAccumulateRouletteService;

internal class DepositAccumulateRouletteWorkerClient : IDepositAccumulateRouletteWorkerClient
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

	public DepositAccumulateRouletteWorkerClient(
		HttpClient httpClient)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	}

	public async IAsyncEnumerable<RouletteRecordInfo> GetRouletteRecordsAsync(
		Guid promotionId,
		string playerCode,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var response = await _httpClient
			.GetAsync($"/api/roulette/records/{playerCode}/{promotionId}", cancellationToken)
			.ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		var rouletteRecords = await response.Content
			.ReadFromJsonAsync<IEnumerable<RouletteRecordInfo>>(_SerializerOptions, cancellationToken)
			.ConfigureAwait(false);

		rouletteRecords ??= Array.Empty<RouletteRecordInfo>();

		foreach (var info in rouletteRecords)
			yield return info;
	}
}
