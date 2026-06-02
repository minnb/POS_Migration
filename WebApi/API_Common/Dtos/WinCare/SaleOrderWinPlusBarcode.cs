using System.ComponentModel.DataAnnotations;

namespace TCX.API.Common.Dtos.WinCare
{
    public class SaleOrderWinPlusBarcode
    {
        [Required]
        public string WincareBarcode { get; set; }
        [Required]
        public string StoreCode { get; set; }
        [Required]
        public string PosCode { get; set; }
        public decimal TotalCash { get; set; }
    }

    public class SaleOrderWinPlusBarcodeData
    {
        public string BillCode { get; set; }
        public string StoreCode { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public decimal TotalCash { get; set; }
    }

    public class ConfirmCollectedCashRequest: SaleOrderWinPlusBarcode
    {
        [Required]
        public string StaffID { get; set; }
        [Required]
        public string EmployeeCode { get; set; }
        [Required]
        public string BussinessDate { get; set; } //yyyyMMdd
        public string EmployeeName { get; set; }
        public string ShiftNumber { get; set; }
        public string StoreCodeWin { get; set; }
        public string BillCode { get; set; }
    }
}
