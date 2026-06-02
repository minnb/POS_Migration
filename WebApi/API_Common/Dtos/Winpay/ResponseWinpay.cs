using System;

namespace TCX.API.Common.Dtos.Winpay
{
    public class PaymentWinpayResponse
    {
        public long Id { get; set; }
        public long MemberId { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreateTime { get; set; }
        public int Type { get; set; }
        public long Parent_id { get; set; }
        public int Flag { get; set; }
        public string StoreId { get; set; }
        public string PosId { get; set; }
        public string RequestId { get; set; }
        public string OrderId { get; set; }
        public string Wallet_type { get; set; }
    }
}
