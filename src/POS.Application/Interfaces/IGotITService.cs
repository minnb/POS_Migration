using POS.Common.Dtos.PartnerApi;

namespace POS.Application.Interfaces;

public interface IGotITService
{
    Task<Tuple<bool, string, List<DataVoucherPartnerResponse>>> CheckMultiple(
        CheckVoucherPartnerPOSRequest request, CancellationToken ct = default);

    Task<Tuple<bool, string, List<DataVoucherPartnerResponse>>> MarkUseMultiple(
        UpdateStatusVoucherPartnerRequest request, CancellationToken ct = default);
}
