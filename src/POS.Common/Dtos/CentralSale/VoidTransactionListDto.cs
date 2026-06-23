namespace POS.Common.Dtos.CentralSale;

public sealed class VoidTransactionListDto
{
    public string   OrderNo         { get; set; } = string.Empty;
    public DateTime OrderDate       { get; set; }
    public DateTime OrderTime       { get; set; }
    public string   StoreNo         { get; set; } = string.Empty;
    public string   POSTerminalNo   { get; set; } = string.Empty;
    public decimal  DiscountAmount  { get; set; }
    public decimal  AmountInclVAT   { get; set; }
    public int      TransactionType { get; set; }
    public DateTime CreatedDate     { get; set; }
    public string?  MemberCardNo    { get; set; }
    public string?  UserVoid        { get; set; }   // void-specific: nhân viên thực hiện hủy
    public string?  Note            { get; set; }   // void-specific: lý do/ghi chú khi hủy
    public string?  CashierID       { get; set; }   // void-specific: người LẬP đơn gốc (audit so với UserVoid)
    public string?  ShiftNo         { get; set; }   // void-specific: ca làm việc

    /// <summary>
    /// Heuristic phát hiện "tự hủy": người hủy trùng thu ngân lập đơn gốc.
    /// Lưu ý: CashierID (nvarchar 10) và UserVoid (nvarchar 50) có thể khác định dạng ID
    /// → nếu sai thực tế cần map qua bảng user.
    /// </summary>
    public bool IsSelfVoid =>
        !string.IsNullOrWhiteSpace(CashierID) &&
        string.Equals(CashierID?.Trim(), UserVoid?.Trim(), StringComparison.OrdinalIgnoreCase);
}
