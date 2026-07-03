using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
   public class SalesTypeRequestModel
    {
        public string SetDB { get; set; }
        public string ListStoreJson { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string SalesType { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }

    public class SalesTypeResponseModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string SalesTypeName { get; set; }
        public string SalesType { get; set; }
        public double? SalesAmount { get; set; }
        public double? TotalOrder { get; set; }
        public int TotalRow { get; set; }
    }
}
