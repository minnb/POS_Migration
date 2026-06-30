namespace POS.Common.Dtos.Promotion;

/// <summary>1 dòng combo trong danh sách (SpecialComboHeader).</summary>
public class SpecialComboListItemDto
{
    public string Code { get; set; } = string.Empty;
    public string SalesType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal ComboQuantity { get; set; }
    public decimal Amount { get; set; }
    public bool IsMember { get; set; }
    public string MemberCode { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IsEnable { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? CreatedDate { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedDate { get; set; }
    public int Total { get; set; }
}

/// <summary>Header combo cho form tạo/sửa.</summary>
public class SpecialComboHeaderDto
{
    public string Code { get; set; } = string.Empty;          // '' = tạo mới (repo auto-gen)
    public string SalesType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal ComboQuantity { get; set; }
    public decimal Amount { get; set; }
    public bool IsMember { get; set; }
    public string MemberCode { get; set; } = string.Empty;
    public string FromDate { get; set; } = string.Empty;       // dd/MM/yyyy
    public string ToDate { get; set; } = string.Empty;         // dd/MM/yyyy
    public bool IsEnable { get; set; } = true;
}

/// <summary>1 dòng sản phẩm trong combo (gom theo GroupCode).</summary>
public class SpecialComboLineDto
{
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string ItemNo { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string UOM { get; set; } = string.Empty;
    public decimal MinimumQuantity { get; set; }
    public decimal MaximumQuantity { get; set; }
    public int Order { get; set; }

    /// <summary>0 = thường, 1 = mặc định (giá tĩnh), 2 = mặc định + giá động.</summary>
    public int PriceMode { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSendSAP { get; set; } = true;
}

/// <summary>Cửa hàng áp dụng combo.</summary>
public class SpecialComboStoreDto
{
    public string StoreNo { get; set; } = string.Empty;        // mã cửa hàng hoặc "ALL"
    public bool IsEnable { get; set; } = true;
}

/// <summary>Gói request Lưu combo (header + lines + stores).</summary>
public class SpecialComboSaveRequest
{
    public SpecialComboHeaderDto Header { get; set; } = new();
    public List<SpecialComboLineDto> Lines { get; set; } = [];
    public List<SpecialComboStoreDto> Stores { get; set; } = [];
}

/// <summary>Chi tiết combo khi mở sửa.</summary>
public class SpecialComboDetailDto
{
    public SpecialComboHeaderDto Header { get; set; } = new();
    public List<SpecialComboLineDto> Lines { get; set; } = [];
    public List<SpecialComboStoreDto> Stores { get; set; } = [];
}

/// <summary>Bộ lọc danh sách Special Combo.</summary>
public class SpecialComboListFilter
{
    public string? StoreNo { get; set; }
    public string? SalesType { get; set; }
    public string? MemberType { get; set; }
    public string? Status { get; set; }     // "-1"=Tất cả, "1"=Đang áp dụng, "0"=Ngưng
    public string? TextSearch { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; } = 20;
}
