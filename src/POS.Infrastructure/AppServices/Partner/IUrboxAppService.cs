using POS.Common.Dtos.PartnerApi;

namespace POS.Infrastructure.AppServices.Partner;

public interface IUrboxAppService
{
    Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> CheckSerialUrbox(
        CheckVoucherPartnerPOSRequest request, CancellationToken ct = default);

    Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> PayCodelUrbox(
        UpdateStatusVoucherPartnerRequest request, CancellationToken ct = default);
}
