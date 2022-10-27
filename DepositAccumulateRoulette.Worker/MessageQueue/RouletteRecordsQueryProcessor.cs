using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Tgs.MessageQueue;
using Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;
using Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;
using Tgs.ServiceUtility;

using Protos = Tgs.Promotion.DepositAccumulateRoulette.Common.Protos;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.MessageQueue;

public class RouletteRecordsQueryProcessor : IMessageProcessor
{
	private readonly IQueryService<GetRouletteRecordsQuery, RouletteRecordInfo> _queryService;

	public RouletteRecordsQueryProcessor(
		IQueryService<GetRouletteRecordsQuery, RouletteRecordInfo> queryService)
	{
		_queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
	}

	public async ValueTask<ReadOnlyMemory<byte>> HandleAsync(
		ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = RouletteRecordsQueryRequest.Parser.ParseFrom(data.Span);

		var rouletteRecordInfos = await _queryService
			.QueryAsync(
				new GetRouletteRecordsQuery(request.PromotionId, request.PlayerCode),
				cancellationToken)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var records = rouletteRecordInfos.Select(info => new Protos.RouletteRecordInfo
		{
			Id = info.RouletteId,
			Date = info.EventDate,
			ItemIndex = info.ItemIndex,
			CreatedOn = info.CreatedOn.ToTimestamp()
		});

		var response = new RouletteRecordsQueryResponse();
		response.RouletteRecords.AddRange(records);

		return response.ToByteArray();
	}
}
