using Newtonsoft.Json;

namespace POS.Common.Dtos.SetupCoupon;

// ─────────────────────────────────────────────────────────────────────────────
// 8.1 / 8.2 Setup Coupon — DTOs (migrate VCM.BLUEPOS SetupCoupon).
// Newtonsoft.Json; PascalCase giữ nguyên tên field (không cần [JsonProperty]).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Bộ lọc danh sách coupon (8.1).</summary>
public sealed class CouponListFilter
{
    public string? IssueType   { get; set; }               // "" | "Auto" | "Import"
    public string? ItemNo      { get; set; }
    public string? Description { get; set; }
    public string  Status      { get; set; } = "-1";        // "-1" tất cả | "1" hiệu lực | "0" hết hiệu lực
    public int     PageSize    { get; set; } = 10;
    public int     PageNumber  { get; set; }                // 0-based
}

/// <summary>1 dòng danh sách coupon (8.1) — khớp cột SP usp_SetupCoupon_GetList.</summary>
public sealed class CouponListItemDto
{
    public string  ItemNo        { get; set; } = string.Empty;
    public string  Description   { get; set; } = string.Empty;
    public string  Prefix        { get; set; } = string.Empty;
    public int     LenCode       { get; set; }
    public string  IssueType     { get; set; } = string.Empty;
    public int     CharOfNumber  { get; set; }
    public int     CharPosition  { get; set; }
    public string  StartingDate  { get; set; } = string.Empty;  // dd/MM/yyyy
    public string  EndingDate    { get; set; } = string.Empty;  // dd/MM/yyyy
    public int     QtyCoupon     { get; set; }
    public bool    Status        { get; set; }
    [JsonIgnore] public int Total { get; set; }                 // COUNT(*) OVER() — chỉ nội bộ paging
}

// ─────────────────────────────────────────────────────────────────────────────
// Danh sách master Coupon/Voucher (CpnVchBOMHeader) — trang /promotion/coupons.
// Port từ SP legacy GetCpnVchBOMHeaderList (list thẳng Header, KHÔNG join IssueRule).
// Khác với CouponListFilter/CouponListItemDto ở trên (màn "Phát hành Coupon").
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Bộ lọc danh sách master Coupon/Voucher — khớp SP usp_CpnVchBOMHeader_GetList.</summary>
public sealed class CouponHeaderListFilter
{
    public string? KeyWord     { get; set; }               // tìm trong ItemNo | CouponCode | ItemName
    public string? ArticleType { get; set; }               // "" = tất cả; ZCPN | ZVCN | ZVCO | ZCOU
    public string  Status      { get; set; } = "-1";        // "-1" tất cả | "0" còn hiệu lực | "1" hết hiệu lực
    public int     PageSize    { get; set; } = 10;
    public int     PageNumber  { get; set; }                // 0-based
}

/// <summary>1 dòng danh sách master Coupon/Voucher — khớp cột SP usp_CpnVchBOMHeader_GetList.</summary>
public sealed class CouponHeaderListItemDto
{
    public string   ItemNo         { get; set; } = string.Empty;
    public string   ItemName       { get; set; } = string.Empty;
    public string   UnitOfMeasure  { get; set; } = string.Empty;
    public string   ArticleType    { get; set; } = string.Empty;
    public int      DiscountType   { get; set; }               // 1 = %, 2 = Amount (theo ArticleType)
    public decimal  DiscountValue  { get; set; }
    public decimal  MaxAmount      { get; set; }
    public decimal  ValueOfVoucher { get; set; }
    public DateTime StartingDate   { get; set; }
    public DateTime EndingDate     { get; set; }
    public bool     Blocked        { get; set; }
    public string   CouponCode     { get; set; } = string.Empty;
    public int      LimitQty       { get; set; }
    public bool     IsCheckItem    { get; set; }               // 1 = theo sản phẩm; 0 = tổng bill
    public long     Counter        { get; set; }
    public string   Pkey           { get; set; } = string.Empty;
    [JsonIgnore] public int Total  { get; set; }               // COUNT(*) OVER() — chỉ nội bộ paging
}

/// <summary>1 mã coupon con (tab "Danh sách phát hành") — khớp cột SP usp_SetupCoupon_GetCodes.</summary>
public sealed class CouponCodeDto
{
    public string  ItemNo      { get; set; } = string.Empty;
    public string  Code        { get; set; } = string.Empty;
    public bool    Enable      { get; set; }
    public string  Status      { get; set; } = string.Empty;   // SOLD/RDM/EXP/AVL (CpnVchBOMCodeIssue.Status)
    public decimal? AmountUsed { get; set; }                   // số tiền đã redeem (null nếu chưa dùng)
    public string? OrderUsed   { get; set; }                   // OrderNo đã redeem (null nếu chưa dùng)
    [JsonIgnore] public int Total { get; set; }
}

/// <summary>Bộ lọc mã coupon theo ItemNo.</summary>
public sealed class CouponCodeFilter
{
    public string ItemNo     { get; set; } = string.Empty;
    public int    PageSize   { get; set; } = 10;
    public int    PageNumber { get; set; }
}

/// <summary>Sản phẩm áp dụng coupon (CpnVchBOMLine).</summary>
public sealed class CouponItemLineDto
{
    public string ItemNo   { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string UOM      { get; set; } = string.Empty;
    public string Barcode  { get; set; } = string.Empty;
}

/// <summary>Cặp giá trị/nhãn cho dropdown form.</summary>
public sealed class CouponOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string Text  { get; set; } = string.Empty;
}

/// <summary>Dữ liệu dropdown cho form phát hành (SalesType, StoreGroup, UOM).</summary>
public sealed class CouponFormLookupDto
{
    public List<CouponOptionDto> SalesTypes  { get; set; } = [];
    public List<string>          StoreGroups { get; set; } = [];
    public List<CouponOptionDto> Uoms        { get; set; } = [];
}

/// <summary>Chi tiết coupon để nạp vào form khi sửa (header + rule + sản phẩm).</summary>
public sealed class CouponDetailDto
{
    public string ItemNo         { get; set; } = string.Empty;
    public string Description    { get; set; } = string.Empty;
    public string IssueType      { get; set; } = string.Empty;
    public string SaleType       { get; set; } = string.Empty;
    public string StoreGroupCode { get; set; } = string.Empty;
    public string ArticleType    { get; set; } = "ZCPN";
    public string StartingDate   { get; set; } = string.Empty;  // dd/MM/yyyy
    public string EndingDate     { get; set; } = string.Empty;  // dd/MM/yyyy
    public bool   IsCheckItem    { get; set; }
    public string Prefix         { get; set; } = string.Empty;
    public int    LenCode        { get; set; }
    public int    CharOfNumber   { get; set; }
    public int    CharPosition   { get; set; }
    public int    QuantityCode   { get; set; }                  // số mã đã phát sinh trong DB
    // Advanced
    public string  UOM          { get; set; } = string.Empty;
    public int     DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal MaxAmount    { get; set; }
    public int     LimitQty     { get; set; }
    public int     LimitQtyUsed { get; set; }
    public bool    IsMultiUse   { get; set; }
    public bool    IsCheckAPI   { get; set; }
    public bool    Blocked      { get; set; }
    public string  CpnVchType   { get; set; } = string.Empty;

    public List<CouponItemLineDto> Items { get; set; } = [];
}

/// <summary>Request phát hành coupon (8.2). ImportCodes chỉ dùng khi IssueType="Import".</summary>
public sealed class CouponIssueSaveRequest
{
    public string  ItemNo         { get; set; } = string.Empty;  // rỗng = tạo mới
    public string  Description    { get; set; } = string.Empty;
    public string  ArticleType    { get; set; } = "ZCPN";
    public string  SaleType       { get; set; } = string.Empty;
    public string  StoreGroupCode { get; set; } = string.Empty;
    public string  StartingDateStr { get; set; } = string.Empty; // dd/MM/yyyy
    public string  EndingDateStr   { get; set; } = string.Empty; // dd/MM/yyyy
    public string  Prefix         { get; set; } = string.Empty;
    public int     LenCode        { get; set; }
    public string  IssueType      { get; set; } = "Auto";        // "Auto" | "Import"
    public int     CharOfNumber   { get; set; }
    public int     CharPosition   { get; set; }
    public int     Quantity       { get; set; }
    public int     QuantityCodeInDB { get; set; }                // số mã đã có (>0 → không sinh thêm)
    public bool    IsCheckItem    { get; set; }
    public List<CouponItemLineDto> Items       { get; set; } = [];
    public List<string>            ImportCodes { get; set; } = [];
}

/// <summary>Request cài đặt nâng cao (8.2 modal Advanced).</summary>
public sealed class CouponAdvancedSaveRequest
{
    public string  ItemNo         { get; set; } = string.Empty;
    public string  Description    { get; set; } = string.Empty;
    public string  ArticleType    { get; set; } = "ZCPN";
    public string  UOM            { get; set; } = string.Empty;
    public string  IssueType      { get; set; } = "Auto";
    public string  CpnVchType     { get; set; } = string.Empty;
    public int     DiscountType   { get; set; } = 1;
    public double  DiscountValue  { get; set; }
    public double  MaxValue       { get; set; }
    public string  StartingDateStr { get; set; } = string.Empty;
    public string  EndingDateStr   { get; set; } = string.Empty;
    public bool    Blocked        { get; set; }
    public bool    IsMultiUsed    { get; set; }
    public bool    IsCheckAPI     { get; set; }
    public int     LimitQty       { get; set; }
    public int     LimitQtyUsed   { get; set; }
    public string  SaleType       { get; set; } = string.Empty;
    public string  StoreGroupCode { get; set; } = string.Empty;
}

/// <summary>
/// Request PHÁT HÀNH THÊM một lô mã Auto mới cho coupon ĐÃ TỒN TẠI (header không đổi).
/// Chỉ hỗ trợ Auto — KHÔNG áp dụng cho Import. Khớp <c>VoucherIssueMoreRequest</c>.
/// </summary>
public sealed class CouponIssueMoreRequest
{
    public string ItemNo       { get; set; } = string.Empty;   // bắt buộc — coupon đã tồn tại
    public string Prefix       { get; set; } = string.Empty;
    public int    LenCode      { get; set; }
    public int    CharOfNumber { get; set; }
    public int    CharPosition { get; set; }
    public int    Quantity     { get; set; }
}

/// <summary>Kết quả lưu coupon.</summary>
public sealed class CouponSaveResult
{
    public bool   Ok      { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ItemNo  { get; set; } = string.Empty;
}
