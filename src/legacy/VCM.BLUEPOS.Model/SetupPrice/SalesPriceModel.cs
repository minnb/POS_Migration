using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPrice
{
    public class SalesPriceModel
    {
        public string ItemNo { get; set; }
        public string SalesCode { get; set; }
        public System.DateTime StartingDate { get; set; }
        public string CurrencyCode { get; set; }
        public string UnitOfMeasureCode { get; set; }
        public double UnitPrice { get; set; }
        public byte PriceIncludesVAT { get; set; }
        public byte AllowInvoiceDisc { get; set; }
        public string SalesType { get; set; }
        public Nullable<double> MinimumQuantity { get; set; }
        public System.DateTime EndingDate { get; set; }
        public string VariantCode { get; set; }
        public byte AllowLineDisc { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Pkey { get; set; }
    }
}
