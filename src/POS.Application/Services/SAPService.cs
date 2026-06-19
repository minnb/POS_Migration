using System.Net;
using POS.Common;
using POS.Common.Dtos.SAP;
using POS.Common.Dtos.Vouchers;
using POS.Application.Interfaces;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Application.Services;

public sealed class SAPService(ISAPVoucherRepository sapVoucherRepository) : ISAPService
{
    public async Task<ResultResponse> CreateNewVoucherAsync(List<CreateVoucherModel> model, CancellationToken ct = default)
    {
        var results = new List<VoucherStatusResponse>();
        foreach (var item in model)
        {
            var acticleType = item.VoucherNumber.Length >= 7 && item.VoucherNumber[6] == '3'
                ? "ZCPN"
                : "ZVCN";

            var mapped = new VoucherStatusResponse
            {
                Status           = "SOLD",
                Return           = "0",
                ActicleNo        = item.Article_No,
                ActicleType      = acticleType,
                VoucherNumber    = item.VoucherNumber,
                Value            = item.Value.ToString(),
                Voucher_Currency = "VND",
                Validity_From_Date = item.From_Date,
                Expiry_Date      = item.Expiry_Date,
                CompanyCode      = "WCM",
                Partner          = "SAP",
                VoucherType = item.VoucherType,
            };

            var existing = await sapVoucherRepository.GetByVoucherNumberAsync(item.VoucherNumber, ct);
            if (existing != null)
            {
                results.Add(existing);
            }
            else
            {
                await sapVoucherRepository.InsertAsync(mapped, ct);
                results.Add(mapped);
            }
        }

        return new ResultResponse
        {
            Status  = HttpStatusCode.OK,
            Message = "Success",
            Data    = results
        };
    }

    public async Task<ResultResponse> CheckVoucherAsync(string voucherNumber, CancellationToken ct = default)
    {
        var data = await sapVoucherRepository.GetByVoucherNumberAsync(voucherNumber, ct);
        if (data != null)
        {
            if (data.Status == "RDM") 
            {
                data.Return = "1";
                return new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Mã Voucher/Coupon {voucherNumber} đã được sử dụng", Data = data };
            }
            else
            {
                return new ResultResponse { Status = HttpStatusCode.OK, Message = "Success", Data = data };
            }
        }

        return new ResultResponse
        {
            Status  = HttpStatusCode.NotFound,
            Message = "Mã Voucher/Coupon không tồn tại"
        };
    }

    public async Task<ResultResponse> RedeemCpnVchAsync(VoucherUpdateModel model, CancellationToken ct = default)
    {
        var serials = model.ListSeriNo!
            .Select(x => (x.voucherNumber, x.value))
            .ToList();

        var (success, message, results) = await sapVoucherRepository.RedeemVouchersAsync(
            serials, model.OrderNo, ct);

        if (!success)
            return new ResultResponse { Status = HttpStatusCode.BadRequest, Message = message };

        return new ResultResponse { Status = HttpStatusCode.OK, Message = "OK", Data = results };
    }
}
