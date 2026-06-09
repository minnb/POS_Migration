namespace POS.Common.Dtos.CXVoucher;

public class GenerateOTPResponse
{
    public string? errorCode { get; set; }
    public string? errorMessage { get; set; }
    public object? data { get; set; }
}

public class GetVoucherSerialResponse
{
    public string? errorCode { get; set; }
    public string? errorMessage { get; set; }
    public ResponseDataVoucher? data { get; set; }
}

public class ResponseDataVoucher
{
    public string? phoneNo { get; set; }
    public string? otp { get; set; }
    public List<DetailVoucher>? voucherDetails { get; set; }
}

public class DetailVoucher
{
    public string? voucherType { get; set; }
    public string? voucherSerial { get; set; }
}

public class UpdateVoucherStatusPosRequest
{
    public string? StoreNo { get; set; }
    public string? PosID { get; set; }
    public List<VoucherSerialPosRequest>? ListSerial { get; set; }
}

public class VoucherSerialPosRequest
{
    public string? VoucherSerial { get; set; }
}

public class UpdateVoucherStatusCXRequest
{
    public VoucherSerialDataCXRequest? data { get; set; }
}

public class VoucherSerialDataCXRequest
{
    public List<VoucherSerialCXRequest>? voucherDetails { get; set; }
}

public class VoucherSerialCXRequest
{
    public string? voucherStatus { get; set; }
    public string? voucherSerial { get; set; }
}

public class UpdateVoucherStatusResponse
{
    public string? errorCode { get; set; }
    public string? errorMessage { get; set; }
    public ResponseDataUpdateVoucherStatusData? data { get; set; }
}

public class ResponseDataUpdateVoucherStatusData
{
    public List<ResponseDataUpdateVoucherStatusCodeDetail>? voucherErrorDetails { get; set; }
}

public class ResponseDataUpdateVoucherStatusCodeDetail
{
    public string? voucherType { get; set; }
    public string? voucherSerial { get; set; }
    public string? message { get; set; }
}
