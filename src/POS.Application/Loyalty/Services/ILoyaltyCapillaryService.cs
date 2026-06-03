using POS.Application.Loyalty.DTOs.Capillary;
using POS.Application.Shared.DTOs;

namespace POS.Application.Loyalty.Services;

public interface ILoyaltyCapillaryService
{
    /// <summary>
    /// Lấy thông tin hội viên từ Capillary.
    /// memberType: "PHONE" | "ID"
    /// </summary>
    Task<(bool Success, string Message, CapGetCustomerResponse? Data, int StatusCode)>
        GetCustomerDetailAsync(SysWebApiDto config, string storeNo, string posNo, string phoneOrId, string memberType);

    /// <summary>Đăng ký hội viên mới trên Capillary.</summary>
    Task<(bool Success, string Message, CapCustomerRegistrationResponse? Data)>
        CustomerRegistrationAsync(SysWebApiDto config, CapCustomerRegistrationRequest customer, string storeNo);

    /// <summary>Cập nhật thông tin hội viên trên Capillary.</summary>
    Task<(bool Success, string Message, CapCustomerRegistrationResponse? Data)>
        CustomerUpdateAsync(SysWebApiDto config, CapCustomerUpdateRequest customer, string storeNo, string phoneNumber);

    /// <summary>Ghi nhận giao dịch tích điểm lên Capillary.</summary>
    Task<(bool Success, string Message, CapAddTransactionResponse? Data)>
        AddTransactionAsync(SysWebApiDto config, object transaction, string storeNo, string memberCard,
            string orderNo = "", List<string>? programIds = null);

    /// <summary>Lấy lịch sử điểm (ledger) của hội viên.</summary>
    Task<(bool Success, string Message, CapLedgerResponse? Data)>
        GetPointsLedgerAsync(SysWebApiDto config, string storeNo, string memberCard);

    /// <summary>Tiêu điểm hội viên (redeem).</summary>
    Task<(bool Success, string Message, CapPointRedeemResponse? Data)>
        PointsRedeemAsync(SysWebApiDto config, CapPointsRedeemRequest request, string storeNo, string programId);

    /// <summary>Hoàn lại điểm đã tiêu (reverse).</summary>
    Task<(bool Success, string Message, CapPointReverseResponse? Data)>
        PointReverseAsync(SysWebApiDto config, CapPointReverseRequest request, string storeNo);

    /// <summary>Kiểm tra hội viên có thể tiêu X điểm không.</summary>
    Task<(bool Success, string Message, CapPointRedeemableResponse? Data)>
        PointsRedeemableAsync(SysWebApiDto config, string memberCard, int point, string storeNo);

    /// <summary>Kiểm tra hợp lệ coupon trước khi sử dụng.</summary>
    Task<(bool Success, string Message, CapRedemptionResponse? Data, int StatusCode)>
        RedemptionValidationDataAsync(SysWebApiDto config, string store, string mobile, string code, bool details);
}
