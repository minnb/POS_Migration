using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.MasterData
{
    public class SalesOrderTypeListModel
    {    
        public int ID { get; set; }
        public string Code { get; set; }
        public string TenderType { get; set; }   // Chiet khau
        public string TenderTypeName { get; set; }
        public string PaymentTenderType { get; set; }
        public string PaymentTenderTypeName { get; set; }
        public string Description { get; set; }
        public Nullable<double> Percent { get; set; }
        public string ImageName { get; set; }
        public string IsActive { get; set; }
        public string ReturnOrder { get; set; }
        public int Order { get; set; }
        public string HotKey { get; set; }
        public int Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public int Total { get; set; }
        public string BlockedLoyaltyStr { get; set; }  // tich diem loyalty
        public string IsBlockedRedeem { get; set; }
        public string AllowedCardServiceStr { get; set; }  // dich vu the
    }


    public class CreateSalesOrderTypeModel
    {
        public string SalesTypeCode { get; set; }
        public string TenderType { get; set; }   // Chiet khau
        public string PaymentTenderType { get; set; }
        public string Description { get; set; }
        public string Percent { get; set; }
        public string ImageName { get; set; }
        public string IsActive { get; set; }
        public string ReturnOrder { get; set; }
        public int Order { get; set; }
        public string HotKey { get; set; }
        public int Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public string BlockedLoyaltyStr { get; set; }  // tich diem loyalty
        public string IsBlockedRedeem { get; set; }
        public string AllowedCardService { get; set; }  // dich vu the

    }

    public class DeleteSalesOrderTypeModel
    {
        public string SalesTypeCode { get; set; }
    }

    public class UpdateSalesOrderTypeModel
    {
        public string SalesTypeCode { get; set; }
        public string TenderType { get; set; }   // Chiet khau
        public string PaymentTenderType { get; set; }
        public string Description { get; set; }
        public string Percent { get; set; }
        public string ImageName { get; set; }
        public string IsActive { get; set; }
        public string ReturnOrder { get; set; }
        public int Order { get; set; }
        public string HotKey { get; set; }
        public int Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public string BlockedLoyaltyStr { get; set; }  // tich diem loyalty
        public string IsBlockedRedeem { get; set; }
        public string AllowedCardService { get; set; }  // dich vu the

    }

    public class CreateCurrencyRateModel
    {
        public string CurrencyCode { get; set; }
        public DateTime StartingDate { get; set; }
        public string StartingDateStr { get; set; }
        public string StartingDateKey { get; set; }
        public DateTime EndingDate { get; set; }
        public string EndingDateStr { get; set; }
        public double? ExchangeRate { get; set; }
        public string Status { get; set; } 
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public string CheckDateDefault { get; set; }

    }

    public class UpdateCurrencyRateModel
    {
        public string CurrencyCode { get; set; }
        public DateTime StartingDate { get; set; }
        public string StartingDateStr { get; set; }
        public string StartingDateKey { get; set; }
        public DateTime EndingDate { get; set; }
        public string EndingDateStr { get; set; }
        public double? ExchangeRate { get; set; }
        public string Status { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public string CheckDateDefault { get; set; }

    }



}
