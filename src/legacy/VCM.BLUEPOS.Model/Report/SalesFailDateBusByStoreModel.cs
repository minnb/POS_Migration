using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class SalesFailDateBusByStoreModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string SetDB { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public DateTime? BussinessDate { get; set; }
        public DateTime? OrderTime { get; set; }
        public int? CountOrderTotal { get; set; }
        public double? TruocVAT { get; set; }
        public double? SauVAT { get; set; }
        public double? ThueVAT { get; set; }

        public double? SumTruocVAT { get; set; }
        public double? SumSauVAT { get; set; }
        public double? SumVAT { get; set; }

        public int? Total { get; set; }
        public double? SumTotalAmount { get; set; }
        public double? SumTotalOrder { get; set; }
    }
    public class SalesFailDateBusByStoreExcelModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public DateTime? BussinessDate { get; set; }
        public DateTime? OrderTime { get; set; }
        public int? CountOrderTotal { get; set; }
        public double? TotalTruocVAT { get; set; }
        public double? TotalVAT { get; set; }
        public double? AmountTotal { get; set; }
    }
}
