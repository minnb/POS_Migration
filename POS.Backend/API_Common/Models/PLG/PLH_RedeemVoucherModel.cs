using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Models
{
    public class PLH_RedeemVoucherModel
    {
        public string partner { get; set; }
        public List<PLH_RedeemSeriNo> listSeriNo { get; set; }
        public string orderNo { get; set; }
        public string storeNo { get; set; }
        public string posID { get; set; }
        public string staffCode { get; set; }
        public double totalBill { get; set; }
    }

    public class PLH_RedeemSeriNo
    {
        public string seriNo { get; set; }
        public bool isVoucher { get; set; }
        public double value { get; set; }
    }

    public class PLH_RedeemResponse
    {
        public string SeriNo { get; set; }
        public string Message { get; set; }
        public double? Amount { get; set; }
        public bool StatusVoucher { get; set; }
    }

    public class PLH_RedeemCouponModel
    {
        public List<PLH_RedeemCouponResquest> listSeriNo { get; set; }
        public string orderNo { get; set; }
        public string storeNo { get; set; }
        public string posID { get; set; }
        public string staffCode { get; set; }       
    }
    public class PLH_RedeemCouponResquest
    {
        public string seriNo { get; set; }
        public int quantityRedeem { get; set; }
        
    }
    public class PLH_RedeemCouponResponse
    {
        public string SeriNo { get; set; }
        public string Message { get; set; }
        public int? QuantityRemain { get; set; }
        public bool StatusCode { get; set; }
    }

}
