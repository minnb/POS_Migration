using POS.Application.Interfaces;
using POS.Common.Dtos.PartnerApi;
using POS.Infrastructure.AppServices.Interfaces;

namespace POS.Application.Services;

public sealed class UrboxService(
    IUrboxAppService appService
) : IUrboxService
{
    public Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> CheckSerialUrbox(
        CheckVoucherPartnerPOSRequest request, CancellationToken ct = default)
        => appService.CheckSerialUrbox(request, ct);

    public Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> PayCodelUrbox(
        UpdateStatusVoucherPartnerRequest request, CancellationToken ct = default)
        => appService.PayCodelUrbox(request, ct);
}
