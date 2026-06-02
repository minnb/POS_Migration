using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.Winpay
{
    public class GetInfoWinpayPOS
    {
        public string PhoneNumber { get; set; }
        public string Name        { get; set; }
        public string IDCard { get; set; }
        public DateTime DateOfRegistration { get; set; }
        public string FingerprintEnrolled { get; set; }
        public decimal Balance { get; set; }
        public decimal TotalCashBack { get; set; }
        public WithdrawQuotaInfo WithdrawQuotaInfo { get; set; }

    }
    public class WithdrawQuotaInfo
    {
        public decimal QuotaPerDay { get; set; }
        public decimal TotalPerDay { get; set; }
        public decimal QuotaPerWeek { get; set; }
        public decimal TotalPerWeek { get; set; }
        public decimal QuotaPerMonth { get; set; }
        public decimal TotalPerMonth { get; set; }
    }
}
