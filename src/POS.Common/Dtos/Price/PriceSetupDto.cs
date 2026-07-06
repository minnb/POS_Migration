namespace POS.Common.Dtos.Price;

/// <summary>
/// 9.3 Setup Giá — 1 dòng thô đọc từ file Excel import (port từ LoadExcelExamplePriceModel).
/// Tất cả để string, validate/parse ở tầng sau (giữ nguyên hành vi legacy).
/// </summary>
public sealed class PriceImportRow
{
    public string? ItemNo { get; set; }
    public string? Barcode { get; set; }
    public string? Uom { get; set; }
    public string? UnitPrice { get; set; }
    public string? StartingDate { get; set; }
    public string? EndingDate { get; set; }
}

/// <summary>
/// Kết quả validate 1 dòng import (port từ ImportResultSalePriceModel — SQL join Item/ItemUnitOfMeasure/Barcode).
/// ErrorMessage rỗng = hợp lệ. Property khớp alias SQL (Dapper case-insensitive).
/// </summary>
public sealed class PriceImportResultRow
{
    public string? ItemNo { get; set; }
    public string? Uom { get; set; }
    public string? Barcode { get; set; }
    /// <summary>"{ItemNo}-{Description}" — text hiển thị.</summary>
    public string? Text { get; set; }
    public string? Id { get; set; }
    public string? ErrorMessage { get; set; }
    public string? UnitPrice { get; set; }
    public string? StartingDate { get; set; }
    public string? EndingDate { get; set; }
}

/// <summary>
/// Ngữ cảnh cài đặt giá: quyết định SalesType + SalesCode → thành phần Pkey.
/// Pkey = {SalesType}-{ItemNo}-{UOM}-{SalesCode} (giữ nguyên legacy SetupPriceController.SaveItemPrice).
/// </summary>
public sealed class PriceSetupContext
{
    public string SalesType { get; set; } = string.Empty;
    /// <summary>Store No cụ thể hoặc "ALL".</summary>
    public string SalesCode { get; set; } = "ALL";
}

/// <summary>
/// 1 dòng giá người dùng gửi lên để lưu (từ lưới preview/khai báo tay). Ngày dạng dd/MM/yyyy.
/// </summary>
public sealed class PriceSaveRow
{
    public string ItemNo { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string UOM { get; set; } = string.Empty;
    public string? UnitPrice { get; set; }
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
}

/// <summary>
/// 1 dòng giá đã validate + build đầy đủ (service → repository → TVP dbo.SetupSalePriceLineTVP).
/// Pkey đã tính sẵn; defaults (VND/VAT/disc/MinQty/VariantCode) do SP gán.
/// </summary>
public sealed class PriceSaveLine
{
    public string Pkey { get; set; } = string.Empty;
    public string ItemNo { get; set; } = string.Empty;
    public string SalesCode { get; set; } = string.Empty;
    public string SalesType { get; set; } = string.Empty;
    public string UOM { get; set; } = string.Empty;
    public double UnitPrice { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>Kết quả lưu bảng giá (giống Tuple&lt;bool,string&gt; legacy).</summary>
public sealed class PriceSaveResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 9.1 — khoá định vị 1 dòng SalesPrice cho Sửa/Xóa giá. Bảng KHÔNG có cột Id → dùng composite PK
/// (ItemNo, SalesCode, StartingDate, UnitOfMeasureCode) + SalesType (2026-07: 1 item/uom/nhóm giá/ngày
/// hiệu lực có thể có nhiều dòng khác nhau theo hình thức bán hàng — thiếu SalesType sẽ định vị nhầm
/// dòng khi có nhiều SalesType trùng key). SP tự tra Pkey theo khoá này.
/// </summary>
public sealed class PriceRowKey
{
    public string ItemNo { get; set; } = string.Empty;
    public string SalesCode { get; set; } = string.Empty;
    public string UnitOfMeasureCode { get; set; } = string.Empty;
    public DateTime StartingDate { get; set; }
    public string SalesType { get; set; } = string.Empty;
}

/// <summary>Danh sách lookup cho form setup giá.</summary>
public sealed class PriceSetupLookupDto
{
    /// <summary>Hình thức bán hàng — dbo.SalesOrderType (IsActive=1).</summary>
    public List<PriceOptionDto> SalesTypes { get; set; } = [];

    /// <summary>Nhóm giá — DISTINCT dbo.StorePriceGroup (PriceGroupCode/PriceGroupName). = SalesCode khi lưu.</summary>
    public List<PriceOptionDto> PriceGroups { get; set; } = [];

    /// <summary>Loại khai báo — cố định 1 giá trị "Theo ITEM + ĐVT" (chưa mở rộng logic).</summary>
    public List<PriceOptionDto> DeclarationTypes { get; set; } = [];
}

/// <summary>Cặp value/text cho dropdown.</summary>
public sealed class PriceOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
