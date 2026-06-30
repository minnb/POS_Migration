namespace POS.Common.Dtos.CentralMD;

/// <summary>
/// 1 dòng nhân viên trả về từ SP [dbo].[GetEmployeeList] / [dbo].[GetEmployeeList_Export].
/// SP trả sẵn text hiển thị tiếng Việt cho VoidTransaction/TypeGroup/Status, và
/// LastDateModifiedStr đã format dd/MM/yyyy. Cột Password của SP cố ý KHÔNG map vào DTO
/// (không giữ credential POS trên web server / không hiển thị trên dashboard).
/// </summary>
public class EmployeeListItemDto
{
    public string StaffCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string StoreNo { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string VoidTransaction { get; set; } = string.Empty;
    public string TypeGroup { get; set; } = string.Empty;
    public string PermissionGroup { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string HomePhoneNo { get; set; } = string.Empty;
    public string LastDateModifiedStr { get; set; } = string.Empty;

    /// <summary>Chỉ có ở SP danh sách (GetEmployeeList); export không trả.</summary>
    public long? Counter { get; set; }

    /// <summary>Tổng số bản ghi (SP nhồi vào mỗi row để phân trang server-side).</summary>
    public int Total { get; set; }
}

/// <summary>
/// Tham số lọc + phân trang cho danh mục nhân viên. TypeGroup/Status nhận "-1" (Tất cả),
/// "0", "1"; PageNumber 0-based (SP dùng OFFSET @PageSize*@PageNumber).
/// </summary>
public class EmployeeListFilter
{
    public string? StaffCode { get; set; }
    public string? StaffName { get; set; }
    public string? StoreNo { get; set; }
    public string? TypeGroup { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; } = 20;
}
