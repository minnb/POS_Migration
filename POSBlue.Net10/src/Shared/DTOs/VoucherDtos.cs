using Newtonsoft.Json;

namespace VCM.POSBLUE.Shared.DTOs;

// ── api/vc/* — CX Voucher DTOs ────────────────────────────────────────────────

public class CxGenerateOtpResponse
{
    [JsonProperty("errorCode")] public string? errorCode { get; set; }
    [JsonProperty("errorMessage")] public string? errorMessage { get; set; }
    [JsonProperty("data")] public object? data { get; set; }
}

public class CxGetVoucherSerialResponse
{
    [JsonProperty("errorCode")] public string? errorCode { get; set; }
    [JsonProperty("errorMessage")] public string? errorMessage { get; set; }
    [JsonProperty("data")] public CxVoucherSerialData? data { get; set; }
}

public class CxVoucherSerialData
{
    [JsonProperty("phoneNo")] public string? phoneNo { get; set; }
    [JsonProperty("otp")] public string? otp { get; set; }
    [JsonProperty("voucherDetails")] public List<CxDetailVoucher>? voucherDetails { get; set; }
}

public class CxDetailVoucher
{
    [JsonProperty("voucherType")] public string? voucherType { get; set; }
    [JsonProperty("voucherSerial")] public string? voucherSerial { get; set; }
}

public class UpdateVoucherStatusRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public List<VoucherSerialItem>? ListSerial { get; set; }
}

public class VoucherSerialItem
{
    public string? VoucherSerial { get; set; }
}

// CX internal request/response
public class CxUpdateVoucherStatusRequest
{
    [JsonProperty("data")] public CxVoucherStatusData? data { get; set; }
}

public class CxVoucherStatusData
{
    [JsonProperty("voucherDetails")] public List<CxVoucherStatusItem>? voucherDetails { get; set; }
}

public class CxVoucherStatusItem
{
    [JsonProperty("voucherStatus")] public string? voucherStatus { get; set; }
    [JsonProperty("voucherSerial")] public string? voucherSerial { get; set; }
}

public class CxUpdateVoucherStatusResponse
{
    [JsonProperty("errorCode")] public string? errorCode { get; set; }
    [JsonProperty("errorMessage")] public string? errorMessage { get; set; }
    [JsonProperty("data")] public CxVoucherStatusErrorData? data { get; set; }
}

public class CxVoucherStatusErrorData
{
    [JsonProperty("voucherErrorDetails")] public List<CxVoucherStatusErrorDetail>? voucherErrorDetails { get; set; }
}

public class CxVoucherStatusErrorDetail
{
    [JsonProperty("voucherType")] public string? voucherType { get; set; }
    [JsonProperty("voucherSerial")] public string? voucherSerial { get; set; }
    [JsonProperty("message")] public string? message { get; set; }
}

// ── api/vinid/* — VinID TopUp DTOs ───────────────────────────────────────────

public class TopUpMeta
{
    [JsonProperty("code")] public int code { get; set; }
    [JsonProperty("message")] public string? message { get; set; }
}

public class VinIdTopUpCheckMemberResponse
{
    [JsonProperty("meta")] public TopUpMeta? meta { get; set; }
    [JsonProperty("data")] public VinIdTopUpMemberData? data { get; set; }
}

public class VinIdTopUpMemberData
{
    [JsonProperty("full_name")] public string? full_name { get; set; }
    [JsonProperty("phone_number")] public string? phone_number { get; set; }
    [JsonProperty("dob")] public string? dob { get; set; }
    [JsonProperty("gender")] public string? gender { get; set; }
    [JsonProperty("identify")] public string? identify { get; set; }
    [JsonProperty("status")] public string? status { get; set; }
    [JsonProperty("card_status")] public string? card_status { get; set; }
}

public class VinIdTopUpCheckPosResponse
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? DOB { get; set; }
    public string? Gender { get; set; }
    public string? Identify { get; set; }
    public string? ArticleNo { get; set; }
    public string? Status { get; set; }
    public string? CardStatus { get; set; }
}

public class VinIdTopUpPointPosRequest
{
    public string? PhoneNumber { get; set; }
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public int PointAmount { get; set; }
    public string? OrderNo { get; set; }
    public bool IsVAT { get; set; }
    public TopUpReceiptPosInfo? InfoVAT { get; set; }
}

public class TopUpReceiptPosInfo
{
    public string? FullName { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? TaxCode { get; set; }
}

public class VinIdTopUpPointRequest
{
    [JsonProperty("phone_number")] public string? phone_number { get; set; }
    [JsonProperty("store_code")] public string? store_code { get; set; }
    [JsonProperty("pos_code")] public string? pos_code { get; set; }
    [JsonProperty("point_amount")] public int point_amount { get; set; }
    [JsonProperty("order_id")] public string? order_id { get; set; }
    [JsonProperty("txn_id")] public string? txn_id { get; set; }
    [JsonProperty("send_receipt")] public bool send_receipt { get; set; }
    [JsonProperty("receipt_info")] public TopUpReceiptInfo? receipt_info { get; set; }
}

public class TopUpReceiptInfo
{
    [JsonProperty("name")] public string? name { get; set; }
    [JsonProperty("address")] public string? address { get; set; }
    [JsonProperty("email")] public string? email { get; set; }
    [JsonProperty("tax_code")] public string? tax_code { get; set; }
}

public class VinIdTopUpPointResponse
{
    [JsonProperty("meta")] public TopUpMeta? meta { get; set; }
}

public class VinIdTopUpStatusOrderApiResponse
{
    [JsonProperty("meta")] public TopUpMeta? meta { get; set; }
    [JsonProperty("data")] public TopUpStatusOrderData? data { get; set; }
}

public class TopUpStatusOrderData
{
    [JsonProperty("status")] public string? status { get; set; }
    [JsonProperty("type")] public string? type { get; set; }
}

public class VinIdTopUpStatusOrderPosResponse
{
    public string? StatusOrder { get; set; }
    public string? TypeOrder { get; set; }
}

public class EVoucherMeta
{
    [JsonProperty("code")] public int code { get; set; }
    [JsonProperty("message")] public string? message { get; set; }
}

public class VinIdEVoucherVerifyRequest
{
    [JsonProperty("store_code")] public string? store_code { get; set; }
    [JsonProperty("pos_code")] public string? pos_code { get; set; }
    [JsonProperty("serial_number")] public string? serial_number { get; set; }
    [JsonProperty("merchant_staff_id")] public string? merchant_staff_id { get; set; }
}

public class VinIdEVoucherApiResponse
{
    [JsonProperty("meta")] public EVoucherMeta? meta { get; set; }
    [JsonProperty("data")] public EVoucherDataModel? data { get; set; }
}

public class EVoucherDataModel
{
    [JsonProperty("min_order_value")] public double min_order_value { get; set; }
    [JsonProperty("discount_value")] public double discount_value { get; set; }
    [JsonProperty("discount_type")] public string? discount_type { get; set; }
}

public class EVoucherPosResponse
{
    public double MinOrderValue { get; set; }
    public double DiscountValue { get; set; }
    public string? DiscountType { get; set; }
}

public class VinIdEVoucherRevokeResponse
{
    [JsonProperty("meta")] public EVoucherMeta? meta { get; set; }
}

public class VinIdEVoucherRefundPosRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public List<string>? ListserialNumber { get; set; }
}

public class VinIdEVoucherRefundRequest
{
    [JsonProperty("serials")] public List<string>? serials { get; set; }
}

public class VinIdEVoucherMarkUsedPosRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? UserPOS { get; set; }
    public string? OrderNo { get; set; }
    public List<string>? ListSerialNumber { get; set; }
}

public class VinIdEVoucherUsedRequest
{
    [JsonProperty("merchant_code")] public string? merchant_code { get; set; }
    [JsonProperty("store_code")] public string? store_code { get; set; }
    [JsonProperty("pos_code")] public string? pos_code { get; set; }
    [JsonProperty("serial_number")] public string? serial_number { get; set; }
    [JsonProperty("redeem_ref_id")] public string? redeem_ref_id { get; set; }
    [JsonProperty("merchant_voucher_code")] public string? merchant_voucher_code { get; set; }
    [JsonProperty("merchant_reference_id")] public string? merchant_reference_id { get; set; }
    [JsonProperty("merchant_staff_id")] public int merchant_staff_id { get; set; }
    [JsonProperty("extra_data")] public object? extra_data { get; set; }
}

public class MarkUsedSerialResult
{
    public string? SerialNumber { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

public class RevokeSerialResult
{
    public string? SerialNumber { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

public class SerialMarkUsedResponse
{
    public List<MarkUsedSerialResult>? usedList { get; set; }
    public List<RevokeSerialResult>? revokeList { get; set; }
}
