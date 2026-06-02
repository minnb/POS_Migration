using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.TopupVoucherVinID
{
    public class VinIDEVoucherResponse
    {
        public EVoucherMeta meta { get; set; }
        public EvoucherData data { get; set; }
    }
    public class EVoucherMeta
    {
        public int code { get; set; }
        public string message { get; set; }
    }
    public class EvoucherData
    {
        public long purchased_at { get; set; }
        public long expired_at { get; set; }
        public double min_order_value { get; set; }
        public double discount_value { get; set; }
        public string discount_type { get; set; }
        public bool apply_in_holiday { get; set; }
        public string merchant_voucher_code { get; set; }
    }

    public class EvoucherPosResponse
    {
        //public long PurchasedAt { get; set; }
        //public long ExpiredAt { get; set; }
        public double MinOrderValue { get; set; }
        public double DiscountValue { get; set; }
        public string DiscountType { get; set; }
        //public bool ApplyInHoliday { get; set; }
        //public string MerchantVoucherCode { get; set; }
    }

    public class VinIDEVoucherUserResponse
    {
        public EVoucherMeta meta { get; set; }
    }

    public class VinIDEVoucherRevokeResponse
    {
        public EVoucherMeta meta { get; set; }
    }
    public class RevokeResponse
    {
        public string SerialNumber { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }
}
