using Tgs.ServiceUtility;

namespace Tgs.Promotion.DepositAccumulateRoulette.Worker.ApplicationServices;

internal class ManuelDistributeBonusCommandService : ICommandService<ManuelDistributeBonusCommand>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly IDepositAccumulateRouletteServiceClient _rouletteServiceClient;
	private readonly ICommandService<DistributeBonusCommand> _distributeService;

	public ManuelDistributeBonusCommandService(
		ITgsRepository tgsRepository,
		IDepositAccumulateRouletteServiceClient rouletteServiceClient,
		ICommandService<DistributeBonusCommand> distributeService)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_rouletteServiceClient = rouletteServiceClient ?? throw new ArgumentNullException(nameof(rouletteServiceClient));
		_distributeService = distributeService ?? throw new ArgumentNullException(nameof(distributeService));
	}

	public async ValueTask ExecuteAsync(
		ManuelDistributeBonusCommand command,
		CancellationToken cancellationToken = default)
	{
		var rouletteRecordInfo = await _tgsRepository
			.GetRouletteRecordInfoAsync(
				command.RouletteRecordId,
				cancellationToken)
			.ConfigureAwait(false);

		if (rouletteRecordInfo == null)
			return;

		var promotion = await _rouletteServiceClient
				.GetDepositAccumulateRoulettePromotionAsync(rouletteRecordInfo.PromotionId,
					cancellationToken)
				.ConfigureAwait(false);

		if (promotion == null)
			return;

		var turnoverRate = promotion.CampaignDetail.RouletteItems
			.ElementAt(rouletteRecordInfo.ItemIndex)
			.TurnoverRate;

		var bucket = rouletteRecordInfo.BonusAmount * turnoverRate;

		var allRecords = await _tgsRepository.GetRouletteRecordInfosAsync(
				rouletteRecordInfo.PromotionId,
				rouletteRecordInfo.PlayerCode,
				rouletteRecordInfo.EventDate)
			.OrderBy(r => r.CreatedOn)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var usedCount = Array.IndexOf(allRecords, rouletteRecordInfo) + 1;

		await _distributeService
			.ExecuteAsync(
				new DistributeBonusCommand(
					rouletteRecordInfo.PromotionId,
					rouletteRecordInfo.PlayerCode,
					rouletteRecordInfo.EventDate,
					usedCount,
					rouletteRecordInfo.BonusAmount,
					bucket),
				cancellationToken).ConfigureAwait(false);
	}
}
