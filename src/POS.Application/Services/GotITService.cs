using POS.Application.Interfaces;
using POS.Common.Dtos.PartnerApi;
using POS.Infrastructure.AppServices.Interfaces;

namespace POS.Application.Services;

public sealed class GotITService(
    IGotITAppService appService
) : IGotITService
{
    public Task<Tuple<bool, string, List<DataVoucherPartnerResponse>>> CheckMultiple(
        CheckVoucherPartnerPOSRequest request, CancellationToken ct = default)
        => appService.CheckMultiple(request, ct);

    public Task<Tuple<bool, string, List<DataVoucherPartnerResponse>>> MarkUseMultiple(
        UpdateStatusVoucherPartnerRequest request, CancellationToken ct = default)
        => appService.MarkUseMultiple(request, ct);
}
