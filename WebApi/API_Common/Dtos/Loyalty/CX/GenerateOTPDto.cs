using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Loyalty.CX
{
    public class GenerateOTPDto
    {
        public string PhoneNumber { get; set; }
        public string MerchantId { get; set; }
        public string Action { get; set; }
    }
    public class VerifyOTPDto
    {
        public string PhoneNumber { get; set; }
        public string Otp { get; set; }
        public string Action { get; set; }
    }
    public class GenerateOTPData
    {
        public string MessageId { get; set; }
        public string Status { get; set; }
    }
    public class VerifyOTPData
    {
        public string PhoneNumber { get; set; }
        public string Otp { get; set; }
        public bool IsValid { get; set; }
    }
    public class POSVerifyOTPRequest
    {
        [Required]
        public string PosNo { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string Otp { get; set; }
        public string Action { get; set; }
    }
}
