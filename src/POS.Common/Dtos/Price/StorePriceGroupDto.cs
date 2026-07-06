namespace POS.Common.Dtos.Price;

/// <summary>
/// Danh mục nhóm giá — bộ lọc danh sách (bảng dbo.StorePriceGroupHeader).
/// </summary>
public sealed class PriceGroupListFilter
{
    /// <summary>Mã nhóm giá (LIKE) — rỗng = tất cả.</summary>
    public string? PriceGroupCode { get; set; }
    /// <summary>Tên nhóm giá (LIKE) — rỗng = tất cả.</summary>
    public string? PriceGroupName { get; set; }
    public int PageSize { get; set; } = 20;
    public int PageNumber { get; set; }
}

/// <summary>
/// Danh mục nhóm giá — 1 dòng danh sách. StoreCount/Priority lấy từ bảng chi tiết dbo.StorePriceGroup.
/// Tên property khớp cột query trả về (Dapper map theo tên, case-insensitive).
/// </summary>
public sealed class PriceGroupListItemDto
{
    /// <summary>Tổng số bản ghi (query trả cùng mỗi row để server-side paging).</summary>
    public int Total { get; set; }

    public int ID { get; set; }
    public string PriceGroupCode { get; set; } = string.Empty;
    public string? PriceGroupName { get; set; }
    /// <summary>Độ ưu tiên cấp nhóm (đọc từ 1 dòng StorePriceGroup bất kỳ của nhóm).</summary>
    public int Priority { get; set; }
    /// <summary>Số cửa hàng đã gán vào nhóm.</summary>
    public int StoreCount { get; set; }
    public string? CreatedDateStr { get; set; }
}

/// <summary>
/// Danh mục nhóm giá — 1 cửa hàng đã gán vào nhóm (bảng dbo.StorePriceGroup + tên từ dbo.Store).
/// </summary>
public sealed class PriceGroupStoreItemDto
{
    public string Store { get; set; } = string.Empty;
    public string? StoreName { get; set; }
    public int Priority { get; set; }
}

/// <summary>
/// Danh mục nhóm giá — payload lưu (thêm/sửa). Priority áp cho mọi store trong nhóm (cấp nhóm).
/// Stores = danh sách mã cửa hàng gán vào nhóm (add-only: SP chỉ thêm dòng mới, không xóa dòng cũ).
/// </summary>
public sealed class PriceGroupSaveRequest
{
    public string PriceGroupCode { get; set; } = string.Empty;
    public string PriceGroupName { get; set; } = string.Empty;
    public int Priority { get; set; }
    public List<string> Stores { get; set; } = [];
}
