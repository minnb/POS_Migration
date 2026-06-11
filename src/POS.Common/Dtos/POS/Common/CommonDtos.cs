namespace POS.Common.Dtos.POS.Common;

// ─── Business Date ───────────────────────────────────────────────────────────

public class BusinessDateResponse
{
    public DateTime? BussinessDate { get; set; }
    public DateTime? CurrentDate { get; set; }
    public int Status { get; set; }      // 1=OK, 2=behind, 3=error
    public string? Message { get; set; }
}

public class BussinessDateOpenModel
{
    public string? Code { get; set; }
    public string? StoreNo { get; set; }
    public DateTime? BussinessDate { get; set; }
    public string? CreatedUser { get; set; }
    public string? UpdatedUser { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class SignalStoreModel
{
    public int ID { get; set; }
    public string? StoreNO { get; set; }
    public string? POSTerminalID { get; set; }
    public DateTime? BusinessDate { get; set; }
    public DateTime? CreatedDate { get; set; }
}

// ─── Shift ───────────────────────────────────────────────────────────────────

// Khớp ShiftHeaderModel cũ — result SP [API_POS_CHECK_SHIFT_HEADER]
public class ShiftHeaderModel
{
    public string? ShiftCode { get; set; }
    public bool? IsShiftClosed { get; set; }
}

// ─── POS Monitor ─────────────────────────────────────────────────────────────

// Khớp POSMonitorInsertRequest cũ — LƯU Ý: IsOpenBluePos là int (POS gửi 0/1), không phải bool
public class POSMonitorInsertRequest
{
    public string? StoreNo { get; set; }
    public string? IpAddress { get; set; }
    public string? ComputerName { get; set; }
    public string? PosTerminalID { get; set; }
    public string? BluePosVersion { get; set; }
    public DateTime? BluePosVersionUpdate { get; set; }
    public int BluePosDatabaseStatus { get; set; }
    public int IsOpenBluePos { get; set; }
    public DateTime? DateTimePos { get; set; }
    public int IntervalJob { get; set; }
    public DateTime? LastTimeInsertAll { get; set; }
    public DateTime? LastTimeInsertChange { get; set; }
    public string? JobVersion { get; set; }
    public string? ScriptVersion { get; set; }
}

public class POSMonitorInsertResponse
{
    public string? ReturnAction { get; set; }
}

// ─── POS Terminal ─────────────────────────────────────────────────────────────

public class PosTerminalModel
{
    public string? IPAddress { get; set; }
    public string? StoreNo { get; set; }
    public string? TerminalPOS { get; set; }
    public string? TerminalNetworkID { get; set; }
    public string? StyleProfile { get; set; }
    public string? DefaultSalesType { get; set; }
    public string? SalesTypeFilter { get; set; }
    public string? Pkey { get; set; }
    public string? DualDisHost { get; set; }
    public string? BillNoseri { get; set; }
    public string? Placement { get; set; }
    public int StatementMethod { get; set; }
    public byte? TerminalStatement { get; set; }
    public int TerminalConnection { get; set; }
    public string? PrintReceiptLogo { get; set; }
    public string? CustomerDisplayText1 { get; set; }
    public string? CustomerDisplayText2 { get; set; }
    public int PrintReceiptBCType { get; set; }
    public string? InterfaceProfile { get; set; }
}

// ─── POS Data Setup & Version ─────────────────────────────────────────────────

// Khớp POSDataSetupModel cũ (API_Common/Models/Common/POSDataSetupModel.cs) — table CentralMD.POSDataSetup
public class POSDataSetupModel
{
    public string? Code { get; set; }
    public string? Value { get; set; }
}

// Khớp POSVersionModel cũ — table CentralMD.POSVersion (POS parse đủ các field này)
public class POSVersionModel
{
    public string? LastVersion { get; set; }
    public string? CurVersion { get; set; }
    public DateTime? UpdateTime { get; set; }
    public long? Counter { get; set; }
    public string? Source { get; set; }
    public string? Pkey { get; set; }
    public bool? IsUpdate { get; set; }
    public string? Folder { get; set; }
}

// ─── Order Info ───────────────────────────────────────────────────────────────

public class SaleTableModel
{
    public string? TableName { get; set; }
    public string? TableData { get; set; }
}

// TransHeader — dùng để deserialize SaleTableModel.TableData khi TableName="TransHeader"
public class TransHeader
{
    public string? StoreNo { get; set; }
    // TODO: add remaining fields when implementing GetOrderInfo (TransactionNo, POSTerminal, etc.)
}

// Top OrderNo gần nhất theo Store/POS — table CentralSales.TransHeader
public class TransHeaderOrderModel
{
    public string? StoreNo { get; set; }
    public string? POSTerminalNo { get; set; }
    public DateTime? OrderDate { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? OrderNo { get; set; }
    public string? MemberCardNo { get; set; }
    public decimal? AmountInclVAT { get; set; }
}

// ─── Order Transaction Update ─────────────────────────────────────────────────

public class UpdateOrderInfoModel
{
    public object? Header { get; set; }
    public IEnumerable<object> ListLine { get; set; } = [];
}

public class ResponseUpdateTransModel
{
    public bool Status { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ─── EOD ──────────────────────────────────────────────────────────────────────

public class POSEOD_APIModel
{
    public string? POSTerminal { get; set; }
    public string? StoreNo { get; set; }
    public DateTime BussinessDate { get; set; }
    public int TotalSale { get; set; }
}

// ─── Check Total Bill ─────────────────────────────────────────────────────────

// Khớp CheckTotalBillResponse cũ (CommonData.CheckTotalBill)
public class CheckTotalBillResponse
{
    public bool Status { get; set; }
    public string? Description { get; set; }
    public int TotalBillPOS { get; set; }
    public int TotalBillCentral { get; set; }
}

// ─── Kios ──────────────────────────────────────────────────────────────────────

// Request từ POS (outer wrapper)
public class KiosInsertSalePOSRequest
{
    public string StoreNo { get; set; } = string.Empty;
    public string PosID { get; set; } = string.Empty;
    public string SaleJson { get; set; } = string.Empty;
}

// Request tới KIOS DB sau khi parse SaleJson
public class KiosInsertSaleRequest
{
    public string StoreNo { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Json { get; set; } = string.Empty;
}

// JSON wrapper trong SaleJson field
public class SyncSaleObject
{
    public string? Type { get; set; }
    public object? Data { get; set; }
}

public class LogSaleKiosModel
{
    public string StoreNo { get; set; } = string.Empty;
    public string PosID { get; set; } = string.Empty;
    public string RequestPOS { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string RequestAPI { get; set; } = string.Empty;
    public string ResponsePOS { get; set; } = string.Empty;
    public string ResponseAPI { get; set; } = string.Empty;
}

public class KiosCheckOrderResponse
{
    public string? OrderNo { get; set; }
    public string? Status { get; set; }
    // TODO: add remaining fields from KiosCheckOrder result
}

// ─── Document / Insurance / Coupon ───────────────────────────────────────────

// Khớp POSDocumentNoModel cũ (API_Common/Models/Common/POSDocumentNoModel.cs)
public class POSDocumentNoModel
{
    public string? StoreNo { get; set; }
    public string? POSTerminal { get; set; }
    public string? LastNumber { get; set; }
    public DateTime? LastDateTime { get; set; }
    public string? DocumentType { get; set; }
}

public class InsuranceModel
{
    public string? ReceiptNo { get; set; }
    // TODO: add remaining fields from GetInsurance result
}

public class TransCpnVchIssueModel
{
    public string? ArticleNo { get; set; }
    public string? VoucherType { get; set; }
    public int? MaxQtyUse { get; set; }
    public int? QtyUse { get; set; }
}

// ─── Logging ──────────────────────────────────────────────────────────────────

// LogAPIModel — dùng cho VinIDBLO.WriteLogAPI (SendCodeReward action)
public class LogAPIModel
{
    public string? CardNumber { get; set; }
    public string? StoreNo { get; set; }
    public string? POSTerminal { get; set; }
    public string? InvoiceNo { get; set; }
    public string? ActionType { get; set; }
    public string? RequestPOS { get; set; }
    public string? RequestXML { get; set; }
    public string? ResponseXML { get; set; }
    public DateTime DateTime { get; set; }
}

// LoggingElastic — POS gửi lên để log qua Kibana (endpoint api/common/logging)
public class LoggingElastic
{
    public string? Endpoint { get; set; }
    public string? PosNo { get; set; }
    public string? Message { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public long ResponseTimeMs { get; set; }
    public int ErrorCode { get; set; }
}
