using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class RevenueOrderSalesByStaffModel
    {
        public int ID { get; set; }
        public string StaffCode { get; set; }
        public string FullName { get; set; }
        public double? AmountTotal { get; set; }
        public int? CountOrderTotal { get; set; }
    }

    public class ExportPDFRevenueOrderSalesModel
    {
        public List<RevenueOrderSalesByStaffModel> lstSaleEmp { get; set; }
        public string StaffCode { get; set; }
        public string StaffName { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string TransDate { get; set; }
    }

    public class ExportExcelRevenueOrderSalesByStaffModel
    {
        public string StaffCode { get; set; }
        public string FullName { get; set; }
        public double? AmountTotal { get; set; }
    }










}
