using Newtonsoft.Json;

namespace POS.Common.Dtos.Price;

/// <summary>
/// 9.1 Danh mục Bảng giá — 1 dòng kết quả từ SP [dbo].[GetSalesPriceList] / [GetSalesPriceList_Export].
/// Tên property khớp cột SP trả về (Dapper map theo tên, case-insensitive). Port từ VCM.BLUEPOS PriceListResponseModel.
/// </summary>
public sealed class PriceListItemDto
{
    /// <summary>Tổng số bản ghi (SP trả cùng mỗi row để server-side paging).</summary>
    public int Total { get; set; }

    public int ID { get; set; }
    public string? BarcodeNo { get; set; }
    /// <summary>Tên hiển thị nhóm giá (PriceGroupName) — SP GetSalesPriceList (2026-07 trở đi).</summary>
    public string? SalesCode { get; set; }
    /// <summary>Mã gốc nhóm giá (PriceGroupCode = SalesPrice.SalesCode) — dùng làm khoá Sửa/Xóa, KHÔNG hiển thị.</summary>
    public string? SalesGroupCode { get; set; }
    /// <summary>Tên hình thức bán hàng (SalesOrderType.Description) — cột "Hình thức".</summary>
    public string? SaleTypeName { get; set; }
    /// <summary>Mã gốc hình thức bán hàng (SalesPrice.SalesType) — dùng làm khoá Sửa/Xóa, KHÔNG hiển thị.</summary>
    public string? SalesTypeCode { get; set; }
    public string? SiteNo { get; set; }
    public string? ItemNo { get; set; }
    public string? ItemNo_PLG { get; set; }
    public string? ItemName { get; set; }
    public string? UnitOfMeasureCode { get; set; }
    public string? UnitPrice { get; set; }
    public string? StartingDateStr { get; set; }
    public string? EndingDateStr { get; set; }
    public string? EndingYearStr { get; set; }
}

/// <summary>
/// Bộ lọc danh mục bảng giá. IsCheck ("Còn hiệu lực"): 1 = chỉ giá còn hiệu lực, 0 = tất cả.
/// </summary>
public sealed class PriceListFilter
{
    public string? ItemNo { get; set; }
    public string? ItemName { get; set; }
    /// <summary>Hình thức bán hàng (SalesOrderType.Code) — rỗng = tất cả.</summary>
    public string? SaleType { get; set; }
    /// <summary>Nhóm giá (StorePriceGroup.PriceGroupCode) — rỗng hoặc "ALL" = tất cả.</summary>
    public string? SalesGroup { get; set; } = "ALL";
    public bool OnlyActive { get; set; }
    public int PageSize { get; set; } = 20;
    public int PageNumber { get; set; }

    [JsonIgnore]
    public int IsCheck => OnlyActive ? 1 : 0;
}
