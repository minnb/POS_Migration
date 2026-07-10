namespace POS.Common.Dtos.Ops;

// Gộp POSTerminal (master config) + POSMonitor (live heartbeat) cho page ops/pos-map.
// POSTerminal là gốc (LEFT JOIN POSMonitor) → liệt kê cả máy chưa từng gửi heartbeat.
public class PosTerminalListDto
{
    // ── POSTerminal (master config) ──────────────────────────────────────────
    public string   No                   { get; set; } = string.Empty;   // PosNo (PK)
    public string   StoreNo              { get; set; } = string.Empty;
    public string?  IPAddress            { get; set; }
    public string?  MACAddress           { get; set; }
    public string?  Description          { get; set; }
    public string?  Placement            { get; set; }
    public string?  PrintReceiptLogo     { get; set; }   // khác rỗng → có in bill
    public string?  CustomerDisplayText1 { get; set; }   // nội dung màn hình phụ
    public string?  CustomerDisplayText2 { get; set; }   // khác rỗng → bật màn hình phụ
    public string?  StyleProfile         { get; set; }   // phân biệt chuỗi cửa hàng
    public string?  BillNoseri           { get; set; }   // mã CQT (cơ quan thuế)
    public string?  DefaultPriceGroup    { get; set; }
    public string?  TerminalNetworkID    { get; set; }
    public int?     AutoLogoffAfter_Min  { get; set; }
    public bool?    Status               { get; set; }   // 1=sử dụng, 0=tắt
    public DateTime? LastDateModified    { get; set; }
    public DateTime? CreatedDate         { get; set; }
    public string?  CreatedBy            { get; set; }
    public DateTime? UpdatedDate         { get; set; }
    public string?  UpdatedBy            { get; set; }

    // ── POSMonitor (live heartbeat — null nếu chưa có dữ liệu) ────────────────
    public string?   ComputerName          { get; set; }
    public string?   BluePosVersion        { get; set; }
    public string?   BluePosVersionUpdate  { get; set; }
    public string?   BluePosDatabaseStatus { get; set; }
    public bool?     IsOpenBluePos         { get; set; }
    public DateTime? DateTimePos           { get; set; }   // mốc heartbeat (last seen)

    // ── MasterDataDownloadLog (lượt tải file zip gần nhất — null nếu chưa từng tải) ──
    public DateTime? LastMasterDataDownloadedAt { get; set; }

    // ── Computed ──────────────────────────────────────────────────────────────
    public bool HasMonitor => DateTimePos.HasValue;
    public bool IsOnline    => DateTimePos.HasValue && (DateTime.Now - DateTimePos.Value).TotalMinutes < 10;
    public bool IsEnabled   => Status == true;
    public bool NeedsUpdate => !string.IsNullOrEmpty(BluePosVersionUpdate)
                               && !string.Equals(BluePosVersion, BluePosVersionUpdate, StringComparison.OrdinalIgnoreCase);
    public bool PrintBill           => !string.IsNullOrWhiteSpace(PrintReceiptLogo);
    public bool ShowCustomerDisplay => !string.IsNullOrWhiteSpace(CustomerDisplayText2);
}
