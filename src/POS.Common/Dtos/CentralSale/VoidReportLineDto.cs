namespace POS.Common.Dtos.CentralSale;

public sealed class VoidReportLineDto
{
    public string   OrderNo            { get; set; } = string.Empty;  // TransVoidLine.DocumentNo
    public int      LineNo             { get; set; }
    public string   VoidType           { get; set; } = string.Empty;  // "InvoiceVoid" | "LineVoid"
    public DateTime ScanTime           { get; set; }
    public string?  StoreNo            { get; set; }
    public string?  POSTerminalNo      { get; set; }
    public string?  ItemNo             { get; set; }
    public string?  ItemName           { get; set; }
    public string?  UnitOfMeasure      { get; set; }
    public decimal  Quantity           { get; set; }
    public decimal  UnitPrice          { get; set; }
    public decimal  DiscountAmount     { get; set; }
    public int      VATPercent         { get; set; }
    public decimal  VATAmount          { get; set; }
    public decimal  LineAmountIncVAT   { get; set; }   // tiền mất thực của riêng dòng này
    public decimal? OrderAmountInclVAT { get; set; }   // InvoiceVoid: tiền mất cả hóa đơn; LineVoid: tổng đơn gốc (KHÔNG bị hủy)
    public int?     TransactionType    { get; set; }
    public string?  UserVoid           { get; set; }   // COALESCE(TransVoidHeader.UserVoid, TransVoidLine.StaffID)
    public string?  CashierID          { get; set; }   // chỉ có khi InvoiceVoid (header-only column)
    public string?  ShiftNo            { get; set; }   // chỉ có khi InvoiceVoid (header-only column)
    public string?  Note               { get; set; }
    public string?  MemberCardNo       { get; set; }
    public string?  DivisionCode       { get; set; }
    public string?  Barcode            { get; set; }

    /// <summary>
    /// Heuristic phát hiện "tự hủy": người hủy trùng thu ngân lập đơn gốc.
    /// Chỉ có ý nghĩa với InvoiceVoid — CashierID luôn null với LineVoid (không có header).
    /// </summary>
    public bool IsSelfVoid =>
        !string.IsNullOrWhiteSpace(CashierID) &&
        string.Equals(CashierID?.Trim(), UserVoid?.Trim(), StringComparison.OrdinalIgnoreCase);
}
