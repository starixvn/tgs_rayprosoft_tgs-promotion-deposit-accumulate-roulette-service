using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.MessageQueue;

public class RouletteTaskSession : IMessageSession
{
	private readonly RouletteTaskProcessor _rouletteTaskProcessor;

	public RouletteTaskSession(RouletteTaskProcessor rouletteTaskProcessor)
	{
		_rouletteTaskProcessor = rouletteTaskProcessor ?? throw new ArgumentNullException(nameof(rouletteTaskProcessor));
	}

	public async ValueTask HandleAsync(Question question, CancellationToken cancellationToken = default)
	{
		var request = RouletteSessionRequest.Parser.ParseFrom(question.Data.Span);

		var result = await _rouletteTaskProcessor.RequestAsync(request).ConfigureAwait(false);

		var response = new RouletteSessionResponse
		{
			Id = result.Id,
			ItemIndex = result.ItemIndex,
			RemainingCount = result.RemainingCount
		};

		_ = await question.AskAsync(response.ToByteArray(), cancellationToken)
			.AsTask()
			.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken)
			.ConfigureAwait(false);
	}
}
