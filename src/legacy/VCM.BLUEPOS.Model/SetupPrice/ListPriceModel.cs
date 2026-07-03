using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPrice
{
    public class ListPriceModel
    {
        public int Id { get; set; }
        public string ItemNo { get; set; }
        public string SalesCode { get; set; }
        public string SalesType { get; set; }
        public string Status { get; set; }
        public string ItemName { get; set; }
        public string BarcodeNo { get; set; }
        public string UnitOfMeasureCode { get; set; }
        public double? UnitPrice { get; set; }
        public DateTime? StartingDate { get; set; }
        public DateTime? EndingDate { get; set; }       
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public int Total { get; set; }
    }
}
