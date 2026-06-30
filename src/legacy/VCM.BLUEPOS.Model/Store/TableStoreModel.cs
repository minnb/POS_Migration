using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Store
{
    public class TableStoreModel
    {
        public string No { get; set; }
        public Nullable<bool> IssueVoucherOnline { get; set; }
        public string ResponsibilityCenter { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PostCode { get; set; }
        public string StoreManagerID { get; set; }
        public Nullable<System.DateTime> StoreOpenfrom { get; set; }
        public Nullable<System.DateTime> StoreOpento { get; set; }
        public string PhoneNo { get; set; }
        public string CountryCode { get; set; }
        public string LocationCode { get; set; }
        public string CurrencyCode { get; set; }
        public Nullable<byte> StoreOpenAfterMidnight { get; set; }
        public Nullable<System.DateTime> LastDateModified { get; set; }
        public string FunctionalityProfile { get; set; }
        public string MenuProfile { get; set; }
        public string InterfaceProfile { get; set; }
        public string StyleProfile { get; set; }
        public string HardwareProfile { get; set; }
        public string StatementNos { get; set; }
        public Nullable<byte> OneStatementPerDay { get; set; }
        public Nullable<int> StatementMethod { get; set; }
        public Nullable<int> ClosingMethod { get; set; }
        public string RoundingAccount { get; set; }
        public string TotalDiscountTender { get; set; }
        public string PrintReceiptLogo { get; set; }
        public Nullable<int> PrintReceiptBitmapNo { get; set; }
        public Nullable<int> ItemNoOnReceipt { get; set; }
        public string County { get; set; }
        public string EmailAddress { get; set; }
        public string TaxCode { get; set; }
        public string BusinessAreaNo { get; set; }
        public string InfocodeReturn { get; set; }
        public string TenderTypeReturn { get; set; }
        public string BranchNo { get; set; }
        public Nullable<byte> ForEvent { get; set; }
        public string InfocodeAdjustBill { get; set; }
        public string InfocodeAdjustLine { get; set; }
        public string CustomerDefault { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Pkey { get; set; }
        public string OldNo { get; set; }
    }
}
