using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class SalesDetailListByMCHModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string MCHCode { get; set; }
        public string MCHName { get; set; }
        public string MCH3Code { get; set; }  // ItemNo
        public string MCH3Name { get; set; }
        public double? AmountTotal { get; set; }
        public int? SumQuantity { get; set; }
        public string BussinessDate { get; set; }
        public DateTime? BussinessDateTime { get; set; }
    }

    public class ExportExcelSalesDetailListByMCHModel
    {
        public string BussinessDate { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminalNo { get; set; }
        public string OrderNo { get; set; }
        public string SalesTypeName { get; set; }  // hinh thuc ban hang
        public string SalesIsReturn { get; set; } 
        public string MCH2Code { get; set; }
        public string MCH2Name { get; set; }
        public string MCH3Code { get; set; }  // ItemNo
        public string MCH3Name { get; set; }
        public string UnitOfMeasure { get; set; }
        public double? Quantity { get; set; }
        public double? AmountVAT { get; set; }
        
    }







}
