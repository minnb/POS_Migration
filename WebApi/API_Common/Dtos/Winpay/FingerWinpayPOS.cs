using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TCX.API.Common.Dtos.Winpay
{
    public class UpdateFingerWinpayPOS : POSRequestWinpay
    {
        [Required]
        public string PhoneNumber { get; set; }
        public string OperationId { get; set; }
        [Required]
        public string Otp { get; set; }
        public List<FPEnrollments> FPEnrollments { get; set; }
        public string RequestId { get; set; }
    }
    public class VerifyFingerWinpayPOS : POSRequestWinpay
    {
        public string PhoneNumber { get; set; }
        public string TypeCode { get; set; } //DEPOSIT-SALE-WITHDRAW
        public string OperationId { get; set; }
        public string Fingerprint { get; set; }
    }
    public class UpdateFingerWinpay
    {
        public string Phone_number { get; set; }
        public string Action_type { get; set; }
        public string Code { get; set; }
        public string Store_id { get; set; }
        public string Pos_id { get; set; }
        public string Cashier_id { get; set; }
        public List<FPEnrollments> FPEnrollments { get; set; }
    }
    public class VerifyFingerWinpay
    {
        public string Action_type { get; set; }
        public string Phone_number { get; set; }
        public string Store_id { get; set; }
        public string Pos_id { get; set; }
        public string Cashier_id { get; set; }
        public string Fingerprint { get; set; }
    }
}
