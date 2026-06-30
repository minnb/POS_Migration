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
    public int NumOfDays { get; set; }                         // NUMOFDAYS — ngày áp dụng trong tháng
    public string VoucherFromDate { get; set; } = string.Empty;// dd/MM/yyyy (chỉ khi IsVoucher)
    public string VoucherToDate { get; set; } = string.Empty;  // dd/MM/yyyy
    public int VoucherValidDay { get; set; }                   // ZVCDATE_VA
    public int VoucherLimitNumber { get; set; }                // LIMITNR
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
    public decimal Quantity { get; set; }
    public string ScaleType { get; set; } = "C";
}

/// <summary>Dòng điều kiện NHẬN / chiết khấu (SetupPromotionGET).</summary>
public class OfferGetLineDto : IOfferLineItem
{
    public int LineType { get; set; }
    public string No { get; set; } = string.Empty;
    public string GroupCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
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
