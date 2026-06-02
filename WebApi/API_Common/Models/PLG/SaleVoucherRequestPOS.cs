using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class SaleVoucherRequestPOS
    {
        public string partner { get; set; }
        public string seriNo { get; set; }
        public string storeNo { get; set; }
        public string posID { get; set; }
        public string staffCode { get; set; }
        public double salePrice { get; set; }
        public double discountAmount { get; set; }
        public string orderNo { get; set; }//25/08/2022
        public List<SaleVoucherItem> listSeriNo { get; set; }
    }
    public class SaleVoucherItem
    {
        public string seriNo { get; set; }
        public bool isVoucher { get; set; }
        public double salePrice { get; set; }
        public double discountAmount { get; set; }
    }
}
