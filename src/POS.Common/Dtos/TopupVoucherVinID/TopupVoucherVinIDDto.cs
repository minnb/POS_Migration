namespace POS.Common.Dtos.TopupVoucherVinID;

public class ListSerialResponse
{
    public string? SerialNumber { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

public class ListSerialShowPOSResponse
{
    public string? SerialNumber { get; set; }
    public string? Message { get; set; }
}

public class SerialPOSResponse
{
    public List<ListSerialResponse>? usedList { get; set; }
    public List<RevokeResponse>? revokeList { get; set; }
}

public class VinIDEVoucherRefundRequest
{
    public List<string>? serials { get; set; }
}

public class VinIDEVoucherRefundPOSRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public List<string>? ListserialNumber { get; set; }
}

public class VinIDEVoucherResponse
{
    public EVoucherMeta? meta { get; set; }
    public EvoucherData? data { get; set; }
}

public class EVoucherMeta
{
    public int code { get; set; }
    public string? message { get; set; }
}

public class EvoucherData
{
    public long purchased_at { get; set; }
    public long expired_at { get; set; }
    public double min_order_value { get; set; }
    public double discount_value { get; set; }
    public string? discount_type { get; set; }
    public bool apply_in_holiday { get; set; }
    public string? merchant_voucher_code { get; set; }
}

public class EvoucherPosResponse
{
    public double MinOrderValue { get; set; }
    public double DiscountValue { get; set; }
    public string? DiscountType { get; set; }
}

public class VinIDEVoucherUserResponse
{
    public EVoucherMeta? meta { get; set; }
}

public class VinIDEVoucherRevokeResponse
{
    public EVoucherMeta? meta { get; set; }
}

public class RevokeResponse
{
    public string? SerialNumber { get; set; }
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
}

public class VinIDEVoucherRevokeRequest
{
    public string? merchant_code { get; set; }
    public string? serial_number { get; set; }
}

public class VinIDEVoucherRevokePosRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? SerialNumber { get; set; }
}

public class VinIDEVoucherUsedRequest
{
    public string? merchant_code { get; set; }
    public string? store_code { get; set; }
    public string? pos_code { get; set; }
    public string? serial_number { get; set; }
    public string? redeem_ref_id { get; set; }
    public string? merchant_voucher_code { get; set; }
    public string? merchant_reference_id { get; set; }
    public int merchant_staff_id { get; set; }
    public object? extra_data { get; set; }
}

public class VinIDEVoucherUsedPosRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? UserPOS { get; set; }
    public string? OrderNo { get; set; }
    public List<string>? ListSerialNumber { get; set; }
}

public class EVoucherUsedPosRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? UserPOS { get; set; }
    public string? OrderNo { get; set; }
    public string? SerialNumber { get; set; }
}

public class VinIDEVoucherVerifyRequest
{
    public string? store_code { get; set; }
    public string? pos_code { get; set; }
    public string? serial_number { get; set; }
    public string? merchant_staff_id { get; set; }
}

public class VinIDEVoucherVerifyPOSRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public string? SerialNumber { get; set; }
    public string? UserPOS { get; set; }
}

public class VinIDTopUpCheckPosResponse
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

public class VinIDTopUpPoinRequest
{
    public string? phone_number { get; set; }
    public string? store_code { get; set; }
    public string? pos_code { get; set; }
    public int point_amount { get; set; }
    public string? order_id { get; set; }
    public string? txn_id { get; set; }
    public bool send_receipt { get; set; }
    public ReceiptInfo? receipt_info { get; set; }
}

public class ReceiptInfo
{
    public string? name { get; set; }
    public string? address { get; set; }
    public string? email { get; set; }
    public string? tax_code { get; set; }
}

public class VinIDTopUpPoinPosRequest
{
    public string? PhoneNumber { get; set; }
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public int PointAmount { get; set; }
    public string? OrderNo { get; set; }
    public bool IsVAT { get; set; }
    public ReceiptPOSInfo? InfoVAT { get; set; }
}

public class ReceiptPOSInfo
{
    public string? FullName { get; set; }
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? TaxCode { get; set; }
}

public class VinIDTopUpResponse
{
    public TopUpMeta? meta { get; set; }
    public TopUpData? data { get; set; }
}

public class TopUpMeta
{
    public int code { get; set; }
    public string? message { get; set; }
}

public class TopUpData
{
    public string? full_name { get; set; }
    public string? phone_number { get; set; }
    public string? dob { get; set; }
    public string? gender { get; set; }
    public string? identify { get; set; }
    public string? status { get; set; }
    public string? card_status { get; set; }
}

public class VinIDTopUpPointResponse
{
    public TopUpMeta? meta { get; set; }
}

public class VinIDTopUpStatusOrderResponse
{
    public TopUpMeta? meta { get; set; }
    public TopUpStatusOrderData? data { get; set; }
}

public class TopUpStatusOrderData
{
    public string? status { get; set; }
    public string? type { get; set; }
}

public class VinIDTopUpStatusOrderPosResponse
{
    public string? StatusOrder { get; set; }
    public string? TypeOrder { get; set; }
}
