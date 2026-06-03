using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
    public class TransactionVINIDModel
    {
      public string MemberCard { get; set; }
      public string PageNo { get; set; }
      public string TotalRecordPerPage { get; set; }
      public string StartDate { get; set; }
      public string EndDate { get; set; }
    }
    public class TransactionEnquiryRq
    {
        public RqHeader RqHeader { get; set; }
        public CustomerIdentifier CustomerIdentifier { get; set; }
        public string PageNo { get; set; }
        public string TotalRecordPerPage { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
    public class RqHeader
    {
        public string Date { get; set; }
        public string Time { get; set; }
        public string TimeZone { get; set; }
        public string MessageType { get; set; }
        public string VersionNo { get; set; }
        public string AppVersion { get; set; }
        public string MerchantId { get; set; }
        public string TerminalId { get; set; }
        public string AccessToken { get; set; }
        public string StanNo { get; set; }
        public string InvoiceNo { get; set; }
        public string LastHostRefNo { get; set; }
        public string OTP { get; set; }
        //public string RefNo { get; set; }
        public string ChannelId { get; set; }
        public string OriginalInvoiceNo { set; get; }

    }

    public class CustomerIdentifier
    {
        public string IDType { get; set; }
        public string ID { get; set; }
    }
    public class OperatorLoginRq
    {
        public RqHeader RqHeader { get; set; }
        public LoginWsVinIDModel Login { get; set; }
        public CustomerIdentifier CustomerIdentifier { get; set; }

    }
    public class OperatorLoginRs
    {
        public RsHeader RsHeader { get; set; }
        public LoginWsVinIDModel Login { get; set; }
        public CustomerIdentifier CustomerIdentifier { get; set; }
    }
    public class RsHeader
    {
        public string Date { get; set; }
        public string Time { get; set; }
        public string TimeZone { get; set; }
        public string MessageType { get; set; }
        public string VersionNo { get; set; }
        public string AppVersion { get; set; }
        public string MerchantId { get; set; }
        public string TerminalId { get; set; }
        public string ResponseDate { get; set; }
        public string ResponseTime { get; set; }
        public string ResponseTimeZone { get; set; }
        public string ResponseCode { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string AccessToken { get; set; }
        public string StanNo { get; set; }
        public string HostRefNo { get; set; }
        public string LastHostRefNo { get; set; }
        public string ChannelId { get; set; }
        public OTP OTP { get; set; }
        public string InvoiceNo { get; set; }
    }

    public class OTP
    {
        public string OTPCode { get; set; }
    }
    public class TransactionEnquiryRs
    {
        public RsHeader RsHeader { get; set; }
        public CustomerIdentifier CustomerIdentifier { get; set; }
        public string TotalNoOfPage { get; set; }
        public string PageNo { get; set; }
        public TransactionEnquiryDetail[] TransactionEnquiryList { get; set; }
    }
    public class TransactionEnquiryDetail
    {

        public string ReferenceNo { get; set; }
        public string TransactionDate { get; set; }
        public string TransactionTime { get; set; }
        public string AccountNumber { get; set; }
        public string TransactionType { get; set; }
        public string TypeDescription { get; set; }
        public string Points { get; set; }
        public string BranchNo { get; set; }
        public string BranchName { get; set; }
        public string SkuItem { get; set; }
        public string CatalogueItem { get; set; }
        public string Source { get; set; }
        public string RedeemBean { get; set; }
        public string AwardBean { get; set; }
        public string InvoiceNo { get; set; }

    }
}
