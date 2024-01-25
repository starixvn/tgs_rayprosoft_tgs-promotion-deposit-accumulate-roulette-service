using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

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
		}
	};

	private readonly HttpClient _httpClient;

	public DepositAccumulateRouletteServiceClient(
		HttpClient httpClient) => _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

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

	public async IAsyncEnumerable<RouletteRecord> GetRouletteRecordsAsync(
		Guid brandId,
		RouletteRecordQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var queryStrings = HttpUtility.ParseQueryString(string.Empty);

		queryStrings.Add("begin", query.BeginTime.ToString("o"));
		queryStrings.Add("end", query.EndTime.ToString("o"));

		foreach (var promotionId in query.PromotionIds)
			queryStrings.Add("promotionId", promotionId.ToString());

		foreach (var playerCode in query.PlayerCodes)
			queryStrings.Add("playerCode", playerCode);

		var response = await _httpClient
			.GetAsync($"/api/roulette/records/{brandId}?{queryStrings}", cancellationToken)
			.ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		var rouletteRecords = await response.Content
			.ReadFromJsonAsync<IEnumerable<RouletteRecord>>(_SerializerOptions, cancellationToken)
			.ConfigureAwait(false);

		rouletteRecords ??= Array.Empty<RouletteRecord>();

		foreach (var info in rouletteRecords)
			yield return info;
	}

	public async ValueTask<RouletteRecord?> GetRouletteRecordAsync(
		Guid brandId,
		Guid recordId,
		CancellationToken cancellationToken = default)
	{
		var response = await _httpClient
			.GetAsync($"/api/roulette/record/{brandId}/{recordId}", cancellationToken)
			.ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		return response.StatusCode == HttpStatusCode.NoContent
			? null
			: await response.Content
				.ReadFromJsonAsync<RouletteRecord>(_SerializerOptions, cancellationToken)
				.ConfigureAwait(false);
	}
}
