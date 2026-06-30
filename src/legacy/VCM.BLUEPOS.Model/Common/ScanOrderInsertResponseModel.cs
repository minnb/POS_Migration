using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Common
{
    public class ScanOrderInsertResponseModel
    {
        public string StoreNo { get; set; }
        public string OrderNo { get; set; }
        public string ItemNo { get; set; }
        public DateTime OrderDate { get; set; }
        public double TotalAmount { get; set; }
        public bool SalesIsReturn { get; set; }
        public bool IsCentral { get; set; }
        public DateTime OrderTime { get; set; }
        public string ScanUser { get; set; }
        public DateTime ScanDate { get; set; }
        public int IsExists { get; set; }
    }
}
