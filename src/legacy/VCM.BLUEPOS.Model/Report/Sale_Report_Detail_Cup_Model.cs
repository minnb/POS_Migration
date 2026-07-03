using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
  public  class Sale_Report_Detail_Cup_Model
    {
        public string StoreNo { get; set; }
        public string OrderNo { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? OrderTime { get; set; }
        public string ShiftNo { get; set; }
        public string SalesType { get; set; }       
        public string SaleTypeName { get; set; }
        public string SalesIsReturn { get; set; }
        public string SaleIsReturnName { get; set; }
        public string ReturnedOrderNo { get; set; }
        public string ItemNo { get; set; }
        public string Barcode { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public double? Quantity { get; set; }
        public double? UnitPrice { get; set; }
        public double? Amount { get; set; }
        public double? DiscountAmount { get; set; }
        public string VATCode { get; set; }
        public double? VATAmount { get; set; }
        public double? LineAmount { get; set; }
        public int QuantityCup { get; set; }
    }
}
