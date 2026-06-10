using POS.Common.Dtos.PartnerApi;

namespace POS.Application.Interfaces;

public interface IUrboxService
{
    Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> CheckSerialUrbox(
        CheckVoucherPartnerPOSRequest request, CancellationToken ct = default);

    Task<Tuple<bool, string, List<DataVoucherPartnerResponse>, List<UrboxProducts>>> PayCodelUrbox(
        UpdateStatusVoucherPartnerRequest request, CancellationToken ct = default);
}
