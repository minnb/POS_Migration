using Newtonsoft.Json;

namespace POS.Common.Dtos.Voucher;

// ─────────────────────────────────────────────────────────────────────────────
// 8.3 Danh mục Voucher + 8.4 Tra cứu Voucher Phát hành (migrate VCM.BLUEPOS Voucher).
// Newtonsoft.Json; PascalCase giữ nguyên tên field.
//
// ⚠️ IsCheckItem (8.3) NGƯỢC nghĩa với Coupon:
//    true  = áp dụng TỔNG BILL  → KHÔNG có dòng sản phẩm
//    false = áp dụng THEO SẢN PHẨM → có dòng sản phẩm
// ─────────────────────────────────────────────────────────────────────────────

// ── 8.3 Danh mục Voucher ─────────────────────────────────────────────────────

/// <summary>Bộ lọc danh sách voucher (8.3).</summary>
public sealed class VoucherListFilter
{
    public string? ItemNo      { get; set; }
    public string? ItemName    { get; set; }
    public string? Serial      { get; set; }               // CouponCode
    public string? ArticleType { get; set; }
    public string  Status      { get; set; } = "-1";        // "-1" tất cả | "1" hiệu lực | "0" hết hiệu lực
    public int     PageSize    { get; set; } = 10;
    public int     PageNumber  { get; set; }                // 0-based
}

/// <summary>1 dòng danh sách voucher — khớp cột SP usp_SetupVoucher_GetList.</summary>
public sealed class VoucherListItemDto
{
    public string  ItemNo         { get; set; } = string.Empty;
    public string  SerialNo       { get; set; } = string.Empty;
    public string  ItemName       { get; set; } = string.Empty;
    public string  ArticleType    { get; set; } = string.Empty;
    public string  UnitOfMeasure  { get; set; } = string.Empty;
    public int     DiscountType   { get; set; }
    public decimal DiscountValue  { get; set; }
    public decimal ValueOfVoucher { get; set; }
    public decimal MaxAmount      { get; set; }
    public int     LimitQty       { get; set; }
    public bool    IsCheckItem    { get; set; }
    public string  StartingDate   { get; set; } = string.Empty;  // dd/MM/yyyy
    public string  EndingDate     { get; set; } = string.Empty;  // dd/MM/yyyy
    public string  LastDateModified { get; set; } = string.Empty;
    public bool    Status         { get; set; }
    [JsonIgnore] public int Total { get; set; }
}

/// <summary>Sản phẩm áp dụng voucher (CpnVchBOMLine).</summary>
public sealed class VoucherLineDto
{
    public string ItemNo   { get; set; } = string.Empty;   // = LineItemNo
    public string ItemName { get; set; } = string.Empty;
    public string UOM      { get; set; } = string.Empty;
    public string Barcode  { get; set; } = string.Empty;
}

/// <summary>Chi tiết voucher để nạp form khi sửa (header + sản phẩm).</summary>
public sealed class VoucherDetailDto
{
    public string  ItemNo         { get; set; } = string.Empty;
    public string  SerialNo       { get; set; } = string.Empty;
    public string  ItemName       { get; set; } = string.Empty;
    public string  ArticleType    { get; set; } = string.Empty;
    public string  UnitOfMeasure  { get; set; } = string.Empty;
    public int     DiscountType   { get; set; }
    public decimal DiscountValue  { get; set; }
    public decimal ValueOfVoucher { get; set; }
    public decimal MaxAmount      { get; set; }
    public int     LimitQty       { get; set; }
    public bool    IsCheckItem    { get; set; }
    public bool    Blocked        { get; set; }
    public string  StartingDate   { get; set; } = string.Empty;  // dd/MM/yyyy
    public string  EndingDate     { get; set; } = string.Empty;  // dd/MM/yyyy
    public List<VoucherLineDto> Items { get; set; } = [];
}

/// <summary>Request tạo/sửa voucher (8.3). ItemNo rỗng = tạo mới.</summary>
public sealed class VoucherSaveRequest
{
    public string  ItemNo          { get; set; } = string.Empty;
    public string  Serial          { get; set; } = string.Empty;  // CouponCode — bắt buộc
    public string  ItemName        { get; set; } = string.Empty;
    public string  ArticleType     { get; set; } = string.Empty;
    public string  UnitOfMeasure   { get; set; } = string.Empty;
    public int     DiscountType    { get; set; } = 1;             // 1=%, 2=amount
    public decimal DiscountValue   { get; set; }
    public decimal ValueOfVoucher  { get; set; }
    public decimal MaxAmount       { get; set; }
    public int     LimitQty        { get; set; } = 99999999;
    public bool    IsCheckItem     { get; set; }                  // true = tổng bill (no lines)
    public bool    Blocked         { get; set; }
    public string  StartingDateStr { get; set; } = string.Empty;  // dd/MM/yyyy
    public string  EndingDateStr   { get; set; } = string.Empty;  // dd/MM/yyyy
    public List<VoucherLineDto> Items { get; set; } = [];
}

/// <summary>Kết quả lưu voucher.</summary>
public sealed class VoucherSaveResult
{
    public bool   Ok      { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ItemNo  { get; set; } = string.Empty;
}

/// <summary>Dropdown form voucher (ArticleType, UOM).</summary>
public sealed class VoucherFormLookupDto
{
    public List<VoucherOptionDto> ArticleTypes { get; set; } = [];
    public List<VoucherOptionDto> Uoms         { get; set; } = [];
}

public sealed class VoucherOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string Text  { get; set; } = string.Empty;
}

// ── 8.4 Tra cứu Voucher Phát hành (CentralSales, per-store) ──────────────────

/// <summary>Bộ lọc tra cứu voucher phát hành (8.4). StoreNo bắt buộc.</summary>
public sealed class VoucherPublishedFilter
{
    public string  StoreNo       { get; set; } = string.Empty;    // bắt buộc
    public string  FromDate      { get; set; } = string.Empty;    // dd/MM/yyyy
    public string  ToDate        { get; set; } = string.Empty;    // dd/MM/yyyy
    public string  VoucherType   { get; set; } = string.Empty;    // "" | "V" | "R"
    public string  TextSearch    { get; set; } = string.Empty;    // BonusBuy/SerialNo/OrderNo
    public string  StatusVoucher { get; set; } = string.Empty;    // "" | "SOLD" | "EXP"
    public string  SendSAP       { get; set; } = string.Empty;    // "" | "1" | "0"
    public int     PageSize      { get; set; } = 10;
    public int     PageNumber    { get; set; }                    // 0-based
}

/// <summary>1 dòng voucher đã phát hành — khớp cột SP GetTransCpnVchIssueList (@Export=1).</summary>
public sealed class VoucherPublishedItemDto
{
    public string StoreNo          { get; set; } = string.Empty;
    public string PosNo            { get; set; } = string.Empty;
    public string BonusBuy         { get; set; } = string.Empty;
    public string SerialNo         { get; set; } = string.Empty;
    public string OrderNo          { get; set; } = string.Empty;
    public string ArticleNo        { get; set; } = string.Empty;
    public string ItemName         { get; set; } = string.Empty;
    public string ApplyType        { get; set; } = string.Empty;
    public string Status           { get; set; } = string.Empty;
    public string VoucherValue     { get; set; } = string.Empty;
    public string MaxAmount        { get; set; } = string.Empty;
    public string MaxQtyUse        { get; set; } = string.Empty;
    public string MaxQuantityIssue { get; set; } = string.Empty;
    public string TranDateStr      { get; set; } = string.Empty;
    public string IsOffline        { get; set; } = string.Empty;
    public string IsSend           { get; set; } = string.Empty;
    public string IsCheckItem      { get; set; } = string.Empty;
    public string VoucherType      { get; set; } = string.Empty;
    public string FromDateStr      { get; set; } = string.Empty;
    public string EnDateStr        { get; set; } = string.Empty;
    public string CreatedDateStr   { get; set; } = string.Empty;
    [JsonIgnore] public int Total  { get; set; }
}
