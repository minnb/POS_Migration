using System.ComponentModel.DataAnnotations;

namespace TCX.API.Common.Dtos.Winpay
{
    public class WinpayDataPOS : POSRequestWinpay
    {
        public string TerminalCode { get; set; }
        public string TransactionId { get; set; }
        public string InvoiceNum { get; set; }
        public string TransactionDate { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public string OperationId { get; set; }
        public string RequestId { get; set; }
    }
    public class RefundWinpayPOS: WinpayDataPOS
    {

    }
    public class PaymentWinpayPOS: WinpayDataPOS
    {
        public string Fingerprint { get; set; }
    }

    public class WithdrawWinpayPOS: PaymentWinpayPOS
    {
        public bool WithdrawAll { get; set; } //true: all - false
    }
}
