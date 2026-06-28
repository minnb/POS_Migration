using POS.Common;
using POS.Common.Dtos.SAP;

namespace POS.Application.Features.Sap;

public interface ISAPService
{
    Task<ResultResponse> CreateNewVoucherAsync(List<CreateVoucherModel> model, CancellationToken ct = default);
    Task<ResultResponse> CheckVoucherAsync(string voucherNumber, CancellationToken ct = default);
    Task<ResultResponse> RedeemCpnVchAsync(VoucherUpdateModel model, CancellationToken ct = default);
    Task<ResultResponse> UpdateReturnVoucherAsync(List<VoucherUpdateRequest> model, CancellationToken ct = default);
}
