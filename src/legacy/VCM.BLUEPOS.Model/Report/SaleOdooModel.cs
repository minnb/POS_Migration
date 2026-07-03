using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class SaleOdooModel
    {
        public long ID { get; set; }
        public string OrderNo { get; set; }
        public string StoreNo { get; set; }
        public string ItemNo { get; set; }
        public string BarcodeNo { get; set; }
        public string UOM { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public Nullable<double> Quantity { get; set; }
        public Nullable<double> Discount { get; set; }
        public Nullable<double> Amount { get; set; }
        public Nullable<bool> Status { get; set; }
        public string FormatAmount { get { return string.Format("{0:###,###}", Amount); } }
    }
}
