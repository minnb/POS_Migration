using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TCX.API.Common.Dtos.Winpay
{
    public class POSRequestWinpay 
    {
        [Required]
        public string StoreCode { get; set; }
        [Required]
        public string PosCode { get; set; }
    }
    public class RegisterWinpayPOS: POSRequestWinpay
    {
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string Name { get; set; }
        public string IDCard { get; set; }
        public string OperationId { get; set; }
        [Required]
        public string Otp {  get; set; }
        public List<FPEnrollments> FPEnrollments { get; set; }
        public string RequestId { get; set; }
    }
    public class FPEnrollments
    {
        public string FPIndex { get; set; }
        public string Base64Str { get; set; }
    }

    public class RegisterWinpayRequest:MerchantWinpayDto
    {
        public DataRegisterWinpay Data { get; set; }
    }
    public class DataRegisterWinpay
    {
        public string Action_type { get; set; }
        public string Phone_number { get; set; }
        public string Code { get; set; }
        public string IdCard { get; set; }
        public string Name { get; set; }
        public List<FPEnrollments> FPEnrollments { get; set; }
        public string Pos_id { get; set; }
        public string Store_id { get; set; }
        public string Order_id { get; set; }
    }
    public class CloseAccountWinpay: CommonWinpayRequest
    {
        public string Action_type { get; set; }
        public string Phone_number { get; set; }
        public string Fingerprint { get; set; }
        public string Code { get; set; }
    }
    public class UnregisterWinpayPOS : POSRequestWinpay
    {
        public string PhoneNumber { get; set; }
        public string OperationId { get; set; }
        public string Fingerprint { get; set; }
        [Required]
        public string Otp { get; set; }
        public string RequestId { get; set; }
    }

    public class GetBalanceWinpay
    {
        public string Action_type { get; set; }
        public string Phone_number { get; set; }
    }
}
