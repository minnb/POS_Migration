using System.ComponentModel.DataAnnotations;

namespace TCX.API.Common.Dtos.Winpay
{
    public class CashbackWinpayPOS
    {
        [Required]
        public string StoreCode { get; set; }
        [Required]
        public string PosCode { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string OrderNo { get; set; }
        public string TransactionId { get; set; }
        public int Amount { get; set; }

    }
}
