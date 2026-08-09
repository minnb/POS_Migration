namespace POS.Common.Dtos.Promotion;

/// <summary>1 dòng CTKM draft trong danh sách Cài đặt CTKM (bảng SetupPromotionHEADER).</summary>
public class PromotionSetupListItemDto
{
    public string No { get; set; } = string.Empty;          // BBYNR
    public string Description { get; set; } = string.Empty;  // BBYTEXT
    public string OfferType { get; set; } = string.Empty;    // BBYTYPE
    public string SalesType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;       // "0"/"1"/"2"
    public string ValidFrom { get; set; } = string.Empty;    // yyyyMMdd
    public string ValidTo { get; set; } = string.Empty;      // yyyyMMdd
    public bool IsApprove { get; set; }
    public int Total { get; set; }
}

/// <summary>Header CTKM cho form tạo/sửa.</summary>
public class PromotionSetupHeaderDto
{
    public string No { get; set; } = string.Empty;             // BBYNR ('' = tạo mới)
    public string Description { get; set; } = string.Empty;
    public string SalesType { get; set; } = string.Empty;
    public string OfferType { get; set; } = string.Empty;
    public string Status { get; set; } = "1";                  // mặc định Planned
    public string StartingDate { get; set; } = string.Empty;   // dd/MM/yyyy
    public string EndingDate { get; set; } = string.Empty;     // dd/MM/yyyy
    public bool IsVoucher { get; set; }
    public bool IsApprove { get; set; }
    public string ConditionBuy { get; set; } = "AND";          // AND/OR
    public string ConditionGet { get; set; } = "AND";

    // ── Advanced (Phase 2) ──
    public decimal LimitQty { get; set; }                      // LIMIT — số lần được áp dụng
    public bool MemberOnly { get; set; }                       // VINID = 'X'
    public string MemberCode { get; set; } = string.Empty;     // hạng thẻ
    public int PriorityBBY { get; set; } = 1;                  // ZPRIOR
    public int NumOfDays { get; set; }                         // NUMOFDAYS — KHÔNG dùng ở UI nữa (xem ApplyDaysOfMonth), giữ nguyên để không phá SP legacy Setup_Promotion_Insert (Convert(int, NUMOFDAYS) khi Duyệt/publish)
    public List<int> ApplyDaysOfMonth { get; set; } = [];       // NUMOFDAYSLIST (cột mới) — nhiều ngày trong tháng, JSON array, KHÔNG publish sang OfferHeader
    public string VoucherFromDate { get; set; } = string.Empty;// dd/MM/yyyy (chỉ khi IsVoucher)
    public string VoucherToDate { get; set; } = string.Empty;  // dd/MM/yyyy
    public int VoucherValidDay { get; set; }                   // ZVCDATE_VA
    public int VoucherLimitNumber { get; set; }                // LIMITNR
    public int AllowUseAfterDay { get; set; }                  // ZVCDAY_AFTER — dùng sau N ngày kể từ lúc ra bill
    public string AllowUseAfterTime { get; set; } = string.Empty; // ZVCTIME_AFTER

    // ── Lịch áp dụng theo giờ/ngày trong tuần (tab "Thông tin chung") ──
    public string FromTime { get; set; } = string.Empty;       // TIMEFROM — "HH:mm"
    public string ToTime { get; set; } = string.Empty;         // TIMETO — "HH:mm"
    public bool Mon { get; set; } = true;                       // MON — CTKM mới mặc định áp dụng cả tuần
    public bool Tue { get; set; } = true;
    public bool Wed { get; set; } = true;
    public bool Thu { get; set; } = true;                       // cột DB tên THUR
    public bool Fri { get; set; } = true;
    public bool Sat { get; set; } = true;
    public bool Sun { get; set; } = true;

    // ── Giá trị tổng tiền tối thiểu (ngưỡng tổng bill) — tab "Thông tin chung", enable theo IsTotalBill ──
    // Khi IsTotalBill: đây là ngưỡng bill → publish ghi OfferBenefits.StepAmount = MINVALUE.
    public decimal MinValue { get; set; }                       // MINVALUE

    // ── Cờ tổng bill (tự gán theo IsTotalBill của OfferType) — quyết định publish rẽ Get→OfferBenefits ──
    // TOTALMINVALUE=1 → dòng Get thành OfferBenefits (StepAmount=MINVALUE); =0 → OfferGet.
    public bool IsTotalBill { get; set; }                       // TOTALMINVALUE (0/1)

    // ── Giới hạn số lượng KM tối đa được hưởng (Limit by customer bổ trợ) — OfferMaxQuantity.MaxQuantity ──
    // 0 = không giới hạn riêng theo trường này (vẫn chặn bởi Quantity × LimitQty).
    public int MaxQuantity { get; set; }                        // SetupPromotionHEADER.MaxQuantity → OfferMaxQuantity

    // ── Giảm giá tổng bill (whole-bill, cơ chế RIÊNG ZB21/ZB09) — loại trừ với danh sách dòng Get ──
    public bool CheckTotalDiscount { get; set; }                // TOTALDISCOUNT cờ 'X'/''
    public int TotalDiscountType { get; set; }                  // TOTALDISCOUNTTYPE: 0=%,1=Amount,2=Price
    public decimal TotalDiscountValue { get; set; }              // TOTALDISCOUNTVALUE
}

/// <summary>
/// Quy tắc nghiệp vụ theo mã OfferType — bù cho cờ dbo.OfferType có thể lệch với nghiệp vụ
/// (KHÔNG bắt sửa cờ DB). Dùng chung UI (PromotionSetupPage) và Repository (validate server).
/// Căn cứ: publish Setup_Promotion_Insert loại ZB06/ZB13 khỏi OfferBuy; dien_giai.md mục III
/// (OfferBuy KHÔNG áp dụng cho ZB05/ZB10).
/// </summary>
public static class PromotionOfferTypeRules
{
    /// <summary>Loại KHÔNG có phần Sản phẩm mua (ẩn tab Buy) — publish loại khỏi OfferBuy.</summary>
    public static readonly IReadOnlySet<string> BuyHiddenOfferTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ZB06", "ZB13" };

    /// <summary>Loại nghiệp vụ chỉ có OfferGet — cho hiện tab Buy theo cờ nhưng KHÔNG bắt buộc.</summary>
    public static readonly IReadOnlySet<string> BuyOptionalOfferTypes =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ZB05", "ZB10" };

    public static bool IsBuyHidden(string? offerType)
        => !string.IsNullOrEmpty(offerType) && BuyHiddenOfferTypes.Contains(offerType);

    /// <summary>Buy bắt buộc khi cờ IsSetupBuy=1 và loại KHÔNG thuộc BuyHidden/BuyOptional.</summary>
    public static bool IsBuyRequired(string? offerType, bool isSetupBuy)
        => isSetupBuy && !string.IsNullOrEmpty(offerType)
           && !BuyHiddenOfferTypes.Contains(offerType)
           && !BuyOptionalOfferTypes.Contains(offerType);
}

/// <summary>Option Loại CTKM kèm cờ điều khiển UI (khớp data-attrs của &lt;option&gt; legacy SetupMain.cshtml).</summary>
public class OfferTypeOptionDto
{
    public string Value { get; set; } = string.Empty;          // OfferType code
    public string Text { get; set; } = string.Empty;
    public bool IsTotalBill { get; set; }
    public bool IsSetupBuy { get; set; }
    public bool IsSetupGet { get; set; }
    public bool IsVoucher { get; set; }
    public bool IsGift { get; set; }
    public string UserGuide { get; set; } = string.Empty;
}

/// <summary>Phần chung của dòng Buy/Get — để UI tái dùng 1 cell chọn sản phẩm/nhóm.</summary>
public interface IOfferLineItem
{
    int LineType { get; set; }          // 0 = Item, 1 = Group
    string No { get; set; }
    string GroupCode { get; set; }
    string Description { get; set; }
    string UnitOfMeasure { get; set; }
}

/// <summary>Dòng điều kiện MUA (SetupPromotionBUY).</summary>
public class OfferBuyLineDto : IOfferLineItem
{
    public int LineType { get; set; }                          // 0 = Item, 1 = Group
    public string No { get; set; } = string.Empty;             // MAT_NR
    public string GroupCode { get; set; } = string.Empty;      // MATGROUP
    public string Description { get; set; } = string.Empty;    // hiển thị (không lưu)
    public string UnitOfMeasure { get; set; } = string.Empty;  // MEINH
    public decimal Quantity { get; set; } = 1;                 // MAT_QUAN — mặc định 1, publish suy Step=Quantity (Step=0 vỡ phép chia interval calc)
    public string ScaleType { get; set; } = "C";
    // ZB02 combo: DiscountType=2 (Giá cố định) + DiscountValue = giá bán combo (= Minimum Value SAP).
    // ZB07: DiscountValue = ngưỡng giá trị bill để được mua SP KM giá ưu đãi. Loại khác default 0.
    public int DiscountType { get; set; }                      // SetupPromotionBUY.DiscountType (0=%,1=R,2=P)
    public decimal DiscountValue { get; set; }                 // SetupPromotionBUY.DiscountValue
}

/// <summary>Dòng điều kiện NHẬN / chiết khấu (SetupPromotionGET).</summary>
public class OfferGetLineDto : IOfferLineItem
{
    public int LineType { get; set; }
    public string No { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;                 // QTY — mặc định 1, publish suy Step=Quantity (chống Step=0)
    public string ScaleType { get; set; } = "C";
    public int DiscountType { get; set; }                      // 0 = %, 1 = R, 2 = P
    public decimal DiscountValue { get; set; }
}

/// <summary>Nhóm cửa hàng áp dụng (SetupPromotionSITE — lưu theo SITEGROUPCODE).</summary>
public class OfferSiteLineDto
{
    public string SiteGroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;      // hiển thị (không lưu)
}

/// <summary>Gói request Lưu CTKM (header + 3 nhóm dòng).</summary>
public class PromotionSetupSaveRequest
{
    public PromotionSetupHeaderDto Header { get; set; } = new();
    public List<OfferBuyLineDto> BuyRows { get; set; } = [];
    public List<OfferGetLineDto> GetRows { get; set; } = [];
    public List<string> SiteGroupCodes { get; set; } = [];
}

/// <summary>Chi tiết CTKM trả về khi mở sửa.</summary>
public class PromotionSetupDetailDto
{
    public PromotionSetupHeaderDto Header { get; set; } = new();
    public List<OfferBuyLineDto> BuyRows { get; set; } = [];
    public List<OfferGetLineDto> GetRows { get; set; } = [];
    public List<OfferSiteLineDto> SiteRows { get; set; } = [];
}

/// <summary>Bộ lọc danh sách Cài đặt CTKM.</summary>
public class PromotionSetupListFilter
{
    public string? OfferNo { get; set; }
    public string? OfferName { get; set; }
    public string? ApproveStatus { get; set; }   // ""=Tất cả, "1"=Đã duyệt, "0"=Chưa duyệt
    public string? ItemNo { get; set; }          // barcode — khớp SetupPromotionBUY.MAT_NR / SetupPromotionGET.MATERIALCODE
    public string? OfferType { get; set; }        // BBYTYPE
    public string? Status { get; set; }           // STATUS: ""=Tất cả, "0"=Đang áp dụng, "1"=Lên kế hoạch, "2"=Ngưng áp dụng

    /// <summary>CTKM còn hiệu lực từ ngày này trở đi (VALIDTO >= FromDate) — null = bỏ qua điều kiện.</summary>
    public DateTime? FromDate { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; } = 20;
}

/// <summary>Option sản phẩm cho lookup dòng Buy/Get (bảng dbo.Item).</summary>
public class ItemOptionDto
{
    public string No { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Uom { get; set; } = string.Empty;
}

/// <summary>Request tạo/sửa 1 nhóm cửa hàng (SetupGroupSite) từ modal "Cài đặt nhóm cửa hàng".</summary>
public class SiteGroupSaveRequest
{
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string StoreListRaw { get; set; } = string.Empty;    // "1535;1561 2018,1560" hoặc "ALL"
}

/// <summary>1 dòng trong "Danh sách nhóm CH/ST" (sub-tab 2 của modal Site Group).</summary>
public class SiteGroupListItemDto
{
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int StoreCount { get; set; }                          // -1 = ALL
    public bool Status { get; set; }
    public DateTime? LastUpdateDate { get; set; }
    public int Total { get; set; }                               // server paging
}

/// <summary>1 dòng store cụ thể trong 1 nhóm — cho popup "Xem chi tiết" của Site Group.</summary>
public class SiteGroupStoreItemDto
{
    public string StoreNo { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
}

/// <summary>Request tạo/sửa 1 nhóm sản phẩm (SetupGroupItem) từ modal "Cài đặt nhóm sản phẩm".</summary>
public class ItemGroupSaveRequest
{
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public List<string> ItemNos { get; set; } = [];    // đã lọc rỗng/trùng ở UI trước khi gửi
}

/// <summary>1 dòng trong "Danh sách nhóm sản phẩm" (sub-tab 2 của modal Item Group).</summary>
public class ItemGroupListItemDto
{
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public bool Status { get; set; }
    public DateTime? LastUpdateDate { get; set; }
    public int Total { get; set; }                       // server paging
}

/// <summary>1 sản phẩm cụ thể trong 1 nhóm — cho popup "Xem chi tiết" của Item Group.</summary>
public class ItemGroupItemDto
{
    public string No { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Uom { get; set; } = string.Empty;
}
