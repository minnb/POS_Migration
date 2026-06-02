using System.Collections.Generic;

namespace TCX.API.Common.Models
{
    public class CXSalesModelResponse
    {
        public string invoiceNo { get; set; }
        public string cardNo { get; set; }
        public string phoneNo { get; set; }       
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public long transactionTime { get; set; }
        public double totalBillAmount { get; set; }
        public double spendPoints { get; set; }
        public long? referenceNumber { get; set; }
        public double extraEarnByCampaign { get; set; }
        public double currencyRate { get; set; }

        //Bổ sung ngày 23/3/2022
        public string empCode { get; set; }//Mã nhân viên đăng ký gói cước để được hưởng quyền lợi nhân viên
        public bool? masanerPackageInd { get; set; }//Đánh dấu nhân viên có sử dụng gói Masaner (true) hay không (false)
        public double? staffPercentage { get; set; }//Tỷ lệ phần trăm tích cho nhân viên còn làm việc được thiết lập trên CX
        public double? normCustPercentage { get; set; }//Tỷ lệ phần trăm tích cho khách hàng thông thường hoặc nhân viên không còn làm việc được thiết lập trên CX
    }


    public class CXRefundModelResponse
    {
        public string invoiceNo { get; set; }
        public string originalInvoiceNo { get; set; }
        public string cardNo { get; set; }
        public string phoneNo { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public long transactionTime { get; set; }
        public long? referenceNumber { get; set; }
        public double? extraEarnByCampaign { get; set; }
        public double? refundPoint { get; set; }
        public double? currencyRate { get; set; }
        
        //Bổ sung ngày 23/3/2022
        public string empCode { get; set; }//Mã nhân viên đăng ký gói cước để được hưởng quyền lợi nhân viên
        public bool? masanerPackageInd { get; set; }//Đánh dấu nhân viên có sử dụng gói Masaner (true) hay không (false)
        public double? staffPercentage { get; set; }//Tỷ lệ phần trăm tích cho nhân viên còn làm việc được thiết lập trên CX
        public double? normCustPercentage { get; set; }//Tỷ lệ phần trăm tích cho khách hàng thông thường hoặc nhân viên không còn làm việc được thiết lập trên CX

    }

    public class CXSalesV2ModelResponse
    {
        public string clubCode { get; set; }
        public string merchantId { get; set; }
        public string invoiceNo { get; set; }
        public string cardNo { get; set; }
        public string phoneNo { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public string channelId { get; set; }
        public long transactionTime { get; set; }
        public double totalBillAmount { get; set; }
        public double spendPoints { get; set; }
        public long? referenceNumber { get; set; }
        public double currencyRate { get; set; }

        //Bổ sung ngày 23/3/2022
        public string empCode { get; set; }//Mã nhân viên đăng ký gói cước để được hưởng quyền lợi nhân viên
        public bool? masanerPackageInd { get; set; }//Đánh dấu nhân viên có sử dụng gói Masaner (true) hay không (false)
        public double? staffPercentage { get; set; }//Tỷ lệ phần trăm tích cho nhân viên còn làm việc được thiết lập trên CX
        public double? normCustPercentage { get; set; }//Tỷ lệ phần trăm tích cho khách hàng thông thường hoặc nhân viên không còn làm việc được thiết lập trên CX
        public List<CXSaleV2ExtraEarnByCampaign> extraEarnByCampaign { get; set; }

    }
    public class CXSaleV2ExtraEarnByCampaign
    {
        public string loyaltyMerchantId { get; set; }
        public double amount { get; set; }
        public double earnedPoints { get; set; }
    }

    public class CXRefundV2ModelResponse
    {
        public string merchantId { get; set; }
        public string invoiceNo { get; set; }
        public string originalInvoiceNo { get; set; }
        public string cardNo { get; set; }
        public string phoneNo { get; set; }
        public string storeCode { get; set; }
        public string posCode { get; set; }
        public long transactionTime { get; set; }
        public List<CXSaleV2ExtraEarnByCampaign> extraEarnByCampaign { get; set; }
        public double? refundPoint { get; set; }
        public double? currencyRate { get; set; }

        //Bổ sung ngày 23/3/2022
        public string empCode { get; set; }//Mã nhân viên đăng ký gói cước để được hưởng quyền lợi nhân viên
        public bool? masanerPackageInd { get; set; }//Đánh dấu nhân viên có sử dụng gói Masaner (true) hay không (false)
        public double? staffPercentage { get; set; }//Tỷ lệ phần trăm tích cho nhân viên còn làm việc được thiết lập trên CX
        public double? normCustPercentage { get; set; }//Tỷ lệ phần trăm tích cho khách hàng thông thường hoặc nhân viên không còn làm việc được thiết lập trên CX

    }
}
