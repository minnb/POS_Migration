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
