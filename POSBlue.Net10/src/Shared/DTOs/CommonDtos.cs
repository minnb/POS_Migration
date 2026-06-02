namespace VCM.POSBLUE.Shared.DTOs;

// ─────────────────────────────────────────────────────────────────────────────
// Common DTOs — port từ VCM.POSBLUE.Model.Common.*
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Kết quả kiểm tra số lượng phiếu coupon/voucher đã dùng cho một articleNo.</summary>
public class TransCpnVchIssueDto
{
    public string? ArticleNo   { get; set; }
    public string? SiteCode    { get; set; }
    public int     QtyUsed     { get; set; }
    public int     QtyIssued   { get; set; }
}

/// <summary>Ngày kinh doanh (BusinessDate) trả về cho POS.</summary>
public class BusinessDateDto
{
    /// <summary>1 = đúng ngày, 2 = BusinessDate &lt; hôm nay, 3 = lỗi hệ thống.</summary>
    public int       Status       { get; set; }
    public string?   Message      { get; set; }
    public DateTime? BussinessDate { get; set; }
    public DateTime? CurrentDate  { get; set; }
}

/// <summary>Thông tin ca làm việc (ShiftHeader) cho một POS.</summary>
public class ShiftHeaderDto
{
    public string?   SiteCode     { get; set; }
    public string?   POSTerminal  { get; set; }
    public DateTime  BusinessDate { get; set; }
    public bool      IsShiftClosed { get; set; }
}

/// <summary>Request ghi nhận trạng thái POS Monitor (fire-and-forget).</summary>
public class PosMonitorInsertRequest
{
    public string? StoreNo     { get; set; }
    public string? POSTerminal { get; set; }
    public string? Status      { get; set; }
    public string? Note        { get; set; }
    public DateTime? CreatedDate { get; set; }
}

/// <summary>Response ghi nhận POSMonitor.</summary>
public class PosMonitorInsertResponse
{
    public string? ReturnAction { get; set; }
}

/// <summary>Metadata phiên bản phần mềm POS.</summary>
public class PosVersionDto
{
    public string? Version     { get; set; }
    public string? Description { get; set; }
    public string? Url         { get; set; }
}

/// <summary>Một "bảng" dữ liệu đơn hàng trả về theo cấu trúc tên-bảng + JSON.</summary>
public class SaleTableDto
{
    public string? TableName { get; set; }
    public string? TableData { get; set; }
}

/// <summary>Header của giao dịch bán hàng — dùng khi validate đơn hàng theo storeNo.</summary>
public class TransHeaderDto
{
    public string? StoreNo  { get; set; }
    public string? OrderNo  { get; set; }
    public DateTime? TransDate { get; set; }
}

/// <summary>Request số tài liệu POS (DocumentNo list).</summary>
public class PosDocumentNoDto
{
    public string? StoreNo     { get; set; }
    public string? POSTerminal { get; set; }
    public string? DocumentNo  { get; set; }
}

/// <summary>Request cập nhật thông tin đơn hàng trả (ReturnOrder).</summary>
public class UpdateOrderInfoRequest
{
    public object?       Header   { get; set; }
    public IEnumerable<object> ListLine { get; set; } = [];
}

/// <summary>Kết quả cập nhật đơn hàng trả.</summary>
public class ResponseUpdateTransDto
{
    public bool    Status  { get; set; }
    public string? Message { get; set; }
}

/// <summary>Thông tin bảo hiểm gắn với hoá đơn.</summary>
public class InsuranceDto
{
    public string? ReceiptNo  { get; set; }
    public string? POSNo      { get; set; }
    public string? StaffCode  { get; set; }
    public string? InsuranceCode { get; set; }
    public decimal Amount     { get; set; }
}

/// <summary>Request cập nhật EOD (End-of-Day) cho POS.</summary>
public class PosEodRequest
{
    public string?   StoreNo     { get; set; }
    public string?   POSTerminal { get; set; }
    public DateTime? BusinessDate { get; set; }
}

/// <summary>Kết quả kiểm tra tổng số bill của POS trong ngày.</summary>
public class CheckTotalBillDto
{
    public int     ServerTotal { get; set; }
    public int     PosTotal    { get; set; }
    public bool    IsMatched   { get; set; }
    public string? Message     { get; set; }
}

/// <summary>Request kios gửi dữ liệu bán hàng lên Central.</summary>
public class KiosInsertSaleRequest
{
    public string? StoreNo  { get; set; }
    public string? PosID    { get; set; }
    public string? SaleJson { get; set; }
}

/// <summary>Response kiểm tra đơn hàng từ kios.</summary>
public class KiosCheckOrderDto
{
    public bool    IsFound { get; set; }
    public string? Message { get; set; }
    public object? Data    { get; set; }
}

/// <summary>Request gửi reward code về POS.</summary>
public class SendCodeRewardRequest
{
    public string?   StoreNo      { get; set; }
    public string?   PosID        { get; set; }
    public string?   BussinessDate { get; set; }
    public string?   OrderNo      { get; set; }
    public string?   OfferNo      { get; set; }
}

/// <summary>Payload log từ client POS (thay thế Kibana/Elastic legacy).</summary>
public class PosLoggingRequest
{
    public string? ActionType  { get; set; }
    public string? StoreNo     { get; set; }
    public string? POSTerminal { get; set; }
    public string? Message     { get; set; }
    public string? Detail      { get; set; }
    public DateTime? LoggedAt  { get; set; }
}
