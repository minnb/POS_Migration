using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class RevenueOrderSalesByMCHModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string MCHCode { get; set; }
        public string MCHName { get; set; }
        public double? AmountTotal { get; set; }
        public int? OrderTotal { get; set; }
        public DateTime? BussinessDate { get; set; }
    }
    public class DetailRevenueOrderSalesByMCHModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string MCHCode { get; set; }
        public string MCHName { get; set; }
        public string MCH3 { get; set; }
        public string MCH3Name { get; set; }
        public double? AmountTotal { get; set; }
        public int? SumQuantity { get; set; }
        public DateTime? BussinessDate { get; set; }
    }

    public class ExportExcel_RevenueOrderSalesByMCHModel
    {
        //public DateTime? BussinessDate { get; set; }
        public string BussinessDateStr { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string MCHCode { get; set; }
        public string MCHName { get; set; }
        public int? OrderTotal { get; set; }        // so luong
        public double? AmountTotal { get; set; }    // thanh tien

    }


}
