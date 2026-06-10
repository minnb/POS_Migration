using POS.Common.Dtos.PartnerApi;

namespace POS.Infrastructure.AppServices.Interfaces;

public interface IGotITAppService
{
    Task<Tuple<bool, string, List<DataVoucherPartnerResponse>>> CheckMultiple(
        CheckVoucherPartnerPOSRequest request, CancellationToken ct = default);

    Task<Tuple<bool, string, List<DataVoucherPartnerResponse>>> MarkUseMultiple(
        UpdateStatusVoucherPartnerRequest request, CancellationToken ct = default);
}
