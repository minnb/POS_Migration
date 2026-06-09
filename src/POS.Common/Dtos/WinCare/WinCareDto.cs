using System.ComponentModel.DataAnnotations;

namespace POS.Common.Dtos.WinCare;

public class SaleOrderWinPlusBarcode
{
    [Required]
    public string WincareBarcode { get; set; } = string.Empty;
    [Required]
    public string StoreCode { get; set; } = string.Empty;
    [Required]
    public string PosCode { get; set; } = string.Empty;
    public decimal TotalCash { get; set; }
}

public class SaleOrderWinPlusBarcodeData
{
    public string? BillCode { get; set; }
    public string? StoreCode { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public decimal TotalCash { get; set; }
}

public class ConfirmCollectedCashRequest : SaleOrderWinPlusBarcode
{
    [Required]
    public string StaffID { get; set; } = string.Empty;
    [Required]
    public string EmployeeCode { get; set; } = string.Empty;
    [Required]
    public string BussinessDate { get; set; } = string.Empty;
    public string? EmployeeName { get; set; }
    public string? ShiftNumber { get; set; }
    public string? StoreCodeWin { get; set; }
    public string? BillCode { get; set; }
}

public class WinCareResponse
{
    public int StatusCode { get; set; }
    public object? Data { get; set; }
    public string? StatusName { get; set; }
    public string? MessageTechnical { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SentNotifyPOSRequest : SentNotifyUserWincareRequest
{
    public string? OrderNo { get; set; }
}

public class SentNotifyUserWincareRequest
{
    public string? SiteId { get; set; }
    public string? VoucherCode { get; set; }
    public string? EmployeeCode { get; set; }
    public string? RequestTime { get; set; }
    public string? Note { get; set; }
    public int TypeId { get; set; }
}

public class StaffPointsWincareRequest
{
    public string? PosNo { get; set; }
    public string? Barcode { get; set; }
    public string? RequestTime { get; set; }
}

public class StaffPointsWincareData
{
    public string? StaffPointsId { get; set; }
    public string? EmployeeCode { get; set; }
    public string? PhoneNumber { get; set; }
}
