using System.Runtime.CompilerServices;
using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRouletteService.Protos;

namespace Tgs.DepositAccumulateRoulette.Clients;

internal class DepositAccumulateRouletteClient : IDepositAccumulateRouletteClient
{
	private readonly IMessageSender _messageSender;

	public DepositAccumulateRouletteClient(
		IMessageSender messageSender)
	{
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public async ValueTask<RouletteResult> RouletteAsync(
		Guid promotionId,
		string playerCode,
		CancellationToken cancellationToken = default)
	{
		var request = new RouletteRequest
		{
			PromotionId = promotionId,
			PlayerCode = playerCode
		};

		var responseData = await _messageSender.RequestAsync(
				"tgs.promotion.depositaccumulateroulette.roulette.play",
				request.ToByteArray(),
				cancellationToken)
			.ConfigureAwait(false);

		var response = RouletteResponse.Parser.ParseFrom(responseData.Span);

		return new RouletteResult(
			response.Id,
			response.ItemIndex);
	}

	public async IAsyncEnumerable<RouletteRecord> GetRouletteRecordsAsync(
		Guid promotionId,
		string playerCode,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var request = new RouletteRecordsRequest
		{
			PromotionId = promotionId,
			PlayerCode = playerCode
		};

		var responseData = await _messageSender.RequestAsync(
				"tgs.promotion.depositaccumulateroulette.roulette.records.query",
				request.ToByteArray(),
				cancellationToken)
			.ConfigureAwait(false);

		var response = RouletteRecordsResponse.Parser.ParseFrom(responseData.Span);

		var rouletteRecords = response.RouletteRecords
			.Select(record => new RouletteRecord(
				record.Id,
				record.Date,
				record.ItemIndex,
				record.CreatedOn.ToDateTimeOffset()));

		foreach (var record in rouletteRecords)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			yield return record;
		}
	}
}
