using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.Clients;

internal class DepositAccumulateRouletteServiceClient : IDepositAccumulateRouletteServiceClient
{
	private static readonly JsonSerializerOptions _SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
		Converters = { new JsonStringEnumConverter() }
	};

	private readonly HttpClient _httpClient;

	public DepositAccumulateRouletteServiceClient(HttpClient httpClient)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	}

	public async ValueTask<DepositAccumulateRoulettePromotion?> GetDepositAccumulateRoulettePromotionAsync(
		Guid promotionId,
		CancellationToken cancellationToken)
	{
		var response = await _httpClient
			.GetAsync($"/promotion/{promotionId}", cancellationToken)
			.ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

		return JsonSerializer.Deserialize<DepositAccumulateRoulettePromotion>(content, _SerializerOptions);
	}
}
