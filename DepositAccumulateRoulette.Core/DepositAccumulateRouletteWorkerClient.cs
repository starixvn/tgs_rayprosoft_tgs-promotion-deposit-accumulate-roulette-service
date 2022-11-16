using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json;
using DateOnlyTimeOnly.AspNet.Converters;
using System.Web;

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

	public async IAsyncEnumerable<RouletteRecordInfo> QueryRouletteRecordsAsync(
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

	public async IAsyncEnumerable<RouletteRecordInfo> QueryRouletteRecordsAsync(
		Guid brandId,
		RouletteRecordQueryConditions conditions,
		Paging paging,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var queryStrings = HttpUtility.ParseQueryString(string.Empty);

		queryStrings.Add("begin", conditions.TimeRange.Begin.ToString("o"));
		queryStrings.Add("end", conditions.TimeRange.End.ToString("o"));

		foreach (var promotionId in conditions.PromotionIds)
			queryStrings.Add("promotionId", promotionId.ToString());

		foreach (var playerCode in conditions.PlayerCodes)
			queryStrings.Add("playerCode", playerCode);

		if (paging.ShowAll == true)
			queryStrings.Add("all", "true");
		else
		{
			if (paging.Page != null)
				queryStrings.Add("page", paging.Page.ToString());

			if (paging.Rows != null)
				queryStrings.Add("rows", paging.Rows.ToString());
		}

		if (!string.IsNullOrWhiteSpace(paging.SortBy))
		{
			queryStrings.Add("sort", paging.SortBy);

			if (paging.IsDesc == true)
				queryStrings.Add("desc", "true");
		}

		var response = await _httpClient
			.GetAsync($"/api/roulette/records/{brandId}?{queryStrings}", cancellationToken)
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
