namespace POS.Common.Dtos.Promotion;

/// <summary>
/// 1 dòng khuyến mãi trả về từ SP [dbo].[GetPromotionOfferHeaderList].
/// SP trả sẵn Status dạng text ("Có hiệu lực"/"Hết hiệu lực") và các trường ngày đã format
/// dd/MM/yyyy (string). Map theo đúng tên cột response của legacy OfferHearderResponseModel.
/// </summary>
public class OfferHeaderListItemDto
{
    public int ID { get; set; }
    public string BonusbuyNo { get; set; } = string.Empty;
    public string PromotionNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OfferType { get; set; } = string.Empty;
    public string SalesType { get; set; } = string.Empty;
    public string SalesTypeName { get; set; } = string.Empty;
    public string ItemNo { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StyleProfile { get; set; } = string.Empty;
    public string StartingDate { get; set; } = string.Empty;
    public string EndingDate { get; set; } = string.Empty;
    public string LocalSiteGroup { get; set; } = string.Empty;
    public decimal LimitQty { get; set; }
    public string VoucherFromDate { get; set; } = string.Empty;
    public string VoucherToDate { get; set; } = string.Empty;
    public long? Counter { get; set; }
    public string Pkey { get; set; } = string.Empty;
    public string LastDateModified { get; set; } = string.Empty;

    /// <summary>Tổng số bản ghi (SP nhồi vào mỗi row để phân trang server-side).</summary>
    public int Total { get; set; }
}

/// <summary>
/// Tham số lọc + phân trang cho danh mục khuyến mãi. PageNumber 0-based
/// (SP dùng OFFSET @PageSize*@PageNumber). Status: "-1" (Tất cả) / "0" (Có hiệu lực) / "2" (Hết hiệu lực).
/// </summary>
public class OfferListFilter
{
    public string? TextSearch { get; set; }
    public string? PromotionName { get; set; }
    public string? Status { get; set; }
    public string? OfferType { get; set; }
    public string? ItemNo { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; } = 20;
}

/// <summary>Option Value/Text dùng cho dropdown filter (Loại CTKM, Hình thức bán...).</summary>
public class OptionItemDto
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

// ── Modal "Xem chi tiết" (bảng OfferHeader/OfferBuy/OfferGet/OfferBenefits/OfferSite/OfferPriority
// — offer engine "chuẩn hoá" đã publish, KHÁC nhóm SetupPromotion* dùng cho form soạn thảo) ──
// Ported from src/legacy/VCM.BLUEPOS/Controllers/PromotionController.cs:124-222 (6 action GetDetailOffer*)

/// <summary>
/// Chi tiết đầy đủ 1 offer đã publish (tab "Offer Header" trong modal Xem chi tiết) — 1 record duy nhất.
/// Ported from src/legacy/VCM.BLUEPOS.Model/Promotion/PromotionModel.cs:135-214 (DetailOfferHeaderResponseModel).
/// </summary>
public class OfferHeaderDetailDto
{
    public string OfferNo { get; set; } = string.Empty;
    public int Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Status { get; set; }
    public string OfferType { get; set; } = string.Empty;
    public string PriceGroup { get; set; } = string.Empty;
    public string RoundingMethod { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime LastDateModified { get; set; }
    public string ValidationPeriodID { get; set; } = string.Empty;
    public string ValidationDescription { get; set; } = string.Empty;
    public DateTime StartingDate { get; set; }
    public DateTime EndingDate { get; set; }
    public bool BlockPeriodicDiscount { get; set; }
    public double? DealPrice { get; set; }
    public int ShowDealLines { get; set; }
    public string SalesTypeFilter { get; set; } = string.Empty;
    public int SelectionType { get; set; }
    public string CustomerDiscGroup { get; set; } = string.Empty;
    public string MemberValue { get; set; } = string.Empty;
    public string DiscountTrackingNo { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public double? CouponQtyNeeded { get; set; }
    public int MemberType { get; set; }
    public string MemberAttribute { get; set; } = string.Empty;
    public string MemberAttributeValue { get; set; } = string.Empty;
    public bool BlockSalesCommission { get; set; }
    public bool BlockManualPriceChange { get; set; }
    public bool BlockInfoCodeDiscount { get; set; }
    public bool BlockLineDiscountOffer { get; set; }
    public bool BlockTotalDiscountOffer { get; set; }
    public bool BlockTenderTypeDiscount { get; set; }
    public bool BlockMemberPoints { get; set; }
    public int ConditionBuy { get; set; }
    /// <summary>Convert từ ConditionBuy: 1→"OR", còn lại→"AND" (tính ở Repository).</summary>
    public string ConditionBuyStr { get; set; } = string.Empty;
    public bool MemberOnly { get; set; }
    public int ConditionGet { get; set; }
    /// <summary>Convert từ ConditionGet: 1→"OR", còn lại→"AND" (tính ở Repository).</summary>
    public string ConditionGetStr { get; set; } = string.Empty;
    public string NoSeries { get; set; } = string.Empty;
    public string FromTime { get; set; } = string.Empty;
    public string ToTime { get; set; } = string.Empty;
    public bool Mon { get; set; }
    public bool Tue { get; set; }
    public bool Wed { get; set; }
    public bool Thu { get; set; }
    public bool Fri { get; set; }
    public bool Sat { get; set; }
    public bool Sun { get; set; }
    public int NumOfDays { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public string TenderTypeCode { get; set; } = string.Empty;
    public double? TenderTypeValue { get; set; }
    public double? TenderTypeOfferPercent { get; set; }
    public double? TenderTypeOfferAmount { get; set; }
    public string BankCode { get; set; } = string.Empty;
    public string LocalSiteGroup { get; set; } = string.Empty;
    public double? LimitQty { get; set; }
    public DateTime? VoucherFromDate { get; set; }
    public DateTime? VoucherToDate { get; set; }
    public int? VoucherValidDay { get; set; }
    public int? VoucherLimitNumber { get; set; }
    public string? PromotionNo { get; set; }
    public int? PriorityBBY { get; set; }
    public long? Counter { get; set; }
    public string? Pkey { get; set; }
    public double? MinValue { get; set; }
    public int? TotalDiscountType { get; set; }
    public double? TotalDiscountValue { get; set; }
    public bool? IsVoucher { get; set; }
    public bool? IsTotalBill { get; set; }
    public bool? IsGift { get; set; }
    public string? MemberCode { get; set; }
    public double? DiscountAmountMax { get; set; }
    public bool? IsFullPrice { get; set; }
    public string? Remark { get; set; }
    public string? SalesType { get; set; }
}

/// <summary>
/// 1 dòng điều kiện "Sản phẩm mua" của offer đã publish (bảng OfferBuy) — tab "Offer Buy".
/// Ported from src/legacy/VCM.BLUEPOS.Model/Promotion/PromotionModel.cs:216-238 (DetailOfferBuyResponseModel).
/// </summary>
public class OfferBuyDetailLineDto
{
    public string OfferNo { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public int LineType { get; set; }
    public string No { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public int DiscountType { get; set; }
    /// <summary>Convert từ DiscountType: 0→"%", 1→"Amount", 2→"Price" (tính ở Repository).</summary>
    public string DiscountTypeStr { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal Quantity { get; set; }
    public decimal Step { get; set; }
    public string BonusBuyNo { get; set; } = string.Empty;
    public string LineGroup { get; set; } = string.Empty;
    public string? ScaleType { get; set; }
    /// <summary>Convert từ ScaleType: "A"→"From", "B"→"UpTo", "C"→"Equal" (tính ở Repository).</summary>
    public string ScaleTypeStr { get; set; } = string.Empty;
    public long? Counter { get; set; }
    public string? Pkey { get; set; }

    /// <summary>Tổng số bản ghi (SQL nhồi COUNT(*) OVER() vào mỗi row để phân trang server-side).</summary>
    public int Total { get; set; }
}

/// <summary>
/// 1 dòng điều kiện "Sản phẩm khuyến mãi" của offer đã publish (bảng OfferGet) — tab "Offer Get".
/// Cùng cấu trúc cột với OfferBuy (xem centralMD-schema.md). Ported from
/// src/legacy/VCM.BLUEPOS.Model/Promotion/PromotionModel.cs:265-287 (DetailOfferGetResponseModel).
/// </summary>
public class OfferGetDetailLineDto
{
    public string OfferNo { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public int LineType { get; set; }
    public string No { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public int DiscountType { get; set; }
    public string DiscountTypeStr { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal Quantity { get; set; }
    public decimal Step { get; set; }
    public string BonusBuyNo { get; set; } = string.Empty;
    public string LineGroup { get; set; } = string.Empty;
    public string? ScaleType { get; set; }
    public string ScaleTypeStr { get; set; } = string.Empty;
    public long? Counter { get; set; }
    public string? Pkey { get; set; }

    /// <summary>Tổng số bản ghi (SQL nhồi COUNT(*) OVER() vào mỗi row để phân trang server-side).</summary>
    public int Total { get; set; }
}

/// <summary>
/// 1 dòng "quà tặng/lợi ích" của offer đã publish (bảng OfferBenefits) — tab "Offer Benefits".
/// Ported from src/legacy/VCM.BLUEPOS.Model/Promotion/PromotionModel.cs:240-263 (DetailOfferBenefitsResponseModel).
/// </summary>
public class OfferBenefitLineDto
{
    public string OfferNo { get; set; } = string.Empty;
    public int LineNo { get; set; }
    public int Type { get; set; }
    public string No { get; set; } = string.Empty;
    public string VariantCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ValueType { get; set; }
    /// <summary>Convert từ ValueType (tính ở Repository, cùng quy ước DiscountTypeStr).</summary>
    public string ValueTypeStr { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal StepAmount { get; set; }
    public string LineGroup { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public long? Counter { get; set; }
    public string? Pkey { get; set; }

    /// <summary>Tổng số bản ghi (SQL nhồi COUNT(*) OVER() vào mỗi row để phân trang server-side).</summary>
    public int Total { get; set; }
}

/// <summary>
/// 1 cửa hàng áp dụng của offer đã publish (bảng OfferSite JOIN Store) — tab "Offer Site".
/// Ported from src/legacy/VCM.BLUEPOS.Model/Promotion/PromotionModel.cs:289-297 (DetailOfferSiteModel).
/// </summary>
public class OfferSiteLineDetailDto
{
    public string OfferNo { get; set; } = string.Empty;
    public string PriceGroupCode { get; set; } = string.Empty;
    public string StoreNo { get; set; } = string.Empty;
    /// <summary>Store.StyleProfile thô (VM/VMP/FS/KS...).</summary>
    public string StyleProfile { get; set; } = string.Empty;
    /// <summary>Tên kênh hiển thị, map ở Service: VM→WinMart, VMP→WinMart+, FS→FlagShip, KS→Kiosk.</summary>
    public string StyleProfileName { get; set; } = string.Empty;
    public long? Counter { get; set; }
    public string? Pkey { get; set; }

    /// <summary>Tổng số bản ghi (SQL nhồi COUNT(*) OVER() vào mỗi row để phân trang server-side).</summary>
    public int Total { get; set; }
}

/// <summary>
/// 1 dòng độ ưu tiên theo Loại CTKM (bảng OfferPriority) — tab "Offer Priority".
/// LƯU Ý: lọc theo <c>OfferType</c>, KHÔNG phải OfferNo (1 loại CTKM áp dụng cho nhiều offer).
/// Ported from src/legacy/VCM.BLUEPOS.Model/Promotion/PromotionModel.cs:299-306 (DetailOfferPriorityModel).
/// </summary>
public class OfferPriorityLineDto
{
    public string OfferType { get; set; } = string.Empty;
    public int? Priority { get; set; }
    public bool? IsMember { get; set; }
    public bool? IsDuplicate { get; set; }
    public long? Counter { get; set; }
    public string? Pkey { get; set; }

    /// <summary>Tổng số bản ghi (SQL nhồi COUNT(*) OVER() vào mỗi row để phân trang server-side).</summary>
    public int Total { get; set; }
}
