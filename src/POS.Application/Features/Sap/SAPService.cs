using System.Net;
using POS.Common;
using POS.Common.Dtos.SAP;
using POS.Common.Dtos.Vouchers;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Application.Features.Sap;

public sealed class SAPService(
    IVoucherCodeRepository voucherCodeRepository,
    ICentralMDRepository centralMDRepository) : ISAPService
{
    public async Task<ResultResponse> CreateNewVoucherAsync(List<CreateVoucherModel> model, CancellationToken ct = default)
    {
        // Validate ActicleNo tồn tại trong CpnVchBOMHeader (bỏ qua khi Article_No rỗng — giữ hành vi cũ).
        // Kiểm tra toàn bộ TRƯỚC khi tạo để tránh tạo dở dang (loop tạo không có transaction).
        foreach (var item in model)
        {
            if (string.IsNullOrWhiteSpace(item.Article_No)) continue;
            if (!await centralMDRepository.CpnVchBOMHeaderExistsAsync(item.Article_No, ct))
                return new ResultResponse
                {
                    Status  = HttpStatusCode.BadRequest,
                    Message = $"ActicleNo {item.Article_No} không tồn tại"
                };
        }

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

            var created = await voucherCodeRepository.CreateOrGetAsync(mapped, ct);
            results.Add(created);
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
        var data = await voucherCodeRepository.GetByCodeAsync(voucherNumber, ct);
        if (data != null)
        {
            if (data.Status == "RDM")
            {
                data.Return = "1";
                return new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Mã Voucher/Coupon {voucherNumber} đã được sử dụng", Data = data };
            }

            var isExpired = data.Status == "EXP"
                || (DateTime.TryParseExact(data.Expiry_Date, "dd/MM/yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var expiryDate)
                    && expiryDate.Date < DateTime.Today);

            if (isExpired)
            {
                data.Return = "1";
                return new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Mã Voucher/Coupon {voucherNumber} đã hết hạn", Data = data };
            }

            if (data.Status == "AVL")
            {
                return new ResultResponse { Status = HttpStatusCode.BadRequest, Message = $"Mã Voucher/Coupon {voucherNumber} chưa được kích hoạt", Data = data };
            }

            return new ResultResponse { Status = HttpStatusCode.OK, Message = "Success", Data = data };
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

        var (success, message, results) = await voucherCodeRepository.RedeemAsync(
            serials, model.OrderNo, ct: ct);

        if (!success)
            return new ResultResponse { Status = HttpStatusCode.BadRequest, Message = message };

        return new ResultResponse { Status = HttpStatusCode.OK, Message = "OK", Data = results };
    }

    public async Task<ResultResponse> UpdateReturnVoucherAsync(
        List<VoucherUpdateRequest> model, CancellationToken ct = default)
    {
        var serials = model.Select(x => (x.VoucherNumber, AmountRedeem: 0d)).ToList();
        var orderNo = model[0].OrderNo;

        var (success, message, results) = await voucherCodeRepository.RedeemAsync(
            serials, orderNo, requiredVoucherType: "BNMH", ct);

        if (!success)
            return new ResultResponse { Status = HttpStatusCode.BadRequest, Message = message };

        return new ResultResponse { Status = HttpStatusCode.OK, Message = "OK", Data = results };
    }
}
