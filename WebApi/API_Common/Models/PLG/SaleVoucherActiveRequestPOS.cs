using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class SaleVoucherActiveRequestPOS
    {
        public string partner { get; set; }
        public string storeNo { get; set; }
        public string posID { get; set; }
        public string staffCode { get; set; }
        public List<SaleVoucherActiveItem> listSeriNo { get; set; }
    }
    public class SaleVoucherActiveItem
    {
        public string seriNo { get; set; }
        public bool isVoucher { get; set; }
        public bool isActive { get; set; }
        public string validFrom { get; set; }//YYYYMMDD
        public string validTo { get; set; }//YYYYMMDD
    }
}
