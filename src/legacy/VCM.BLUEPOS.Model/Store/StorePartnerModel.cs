using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Store
{
    public class StorePartnerModel
    {
        public string SiteNo { get; set; }
        public string SiteName { get; set; }
        public string StyleProfile { get; set; }
        public string ValueJson
        {
            get
            {
                return JsonConvert.SerializeObject(new ParamJsonStoreList { SiteNo = SiteNo, SiteName = SiteName });
            }
        }
    }

    public class StoreListResponseModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string Address { get; set; }
        public string StoreManagerID { get; set; }
        public string StyleProfile { get; set; }
        public string ProvinceName { get; set; }
        public string ProvinceCode { get; set; }
        public string IssueVoucherOnline { get; set; }
        public string TaxCode { get; set; }
        public string StoreOpenTime { get; set; }
        public string StoreCloseTime { get; set; }
        public string PhoneNo { get; set; }
        public string PasswordWifi { get; set; }
        public string CountryCode { get; set; }
        public string CurrencyCode { get; set; }
        public string PrintReceiptLogo { get; set; }
        public string LastDateModifiedStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public int Total { get; set; }
    }
    public class ExportExcelStoreListModel
    {
        public string StyleProfile { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }        
        public string Address { get; set; }
        public string StoreOpenTime { get; set; }
        public string StoreCloseTime { get; set; }
        public string TaxCode { get; set; }    
        public string PhoneNo { get; set; }
        public string PasswordWifi { get; set; }
        public string ProvinceCode { get; set; }
        public string ProvinceName { get; set; }
        public string IssueVoucherOnline { get; set; }
        public string LastDateModifiedStr { get; set; }
       
    }
    public class DeleteStoreListModel
    {
        public string StoreNo { get; set; }
    }

    public class CreateStoreListModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string Address { get; set; }
        public string TaxCode { get; set; }
        public string OpenTime { get; set; }
        public string CloseTime { get; set; }
        public string PhoneNo { get; set; }
        public string PassWordWifi { get; set; }
        public string StyleProfile { get; set; }
        public string ProvinceNo { get; set; }
        public string VoucherOnline { get; set; }
        public DateTime? LastDateModified { get; set; }
        public string LastDateModifiedStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class UpdateStoreListModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string Address { get; set; }
        public string TaxCode { get; set; }
        public string OpenTime { get; set; }
        public string CloseTime { get; set; }
        public string PhoneNo { get; set; }
        public string PassWordWifi { get; set; }
        public string ProvinceNo { get; set; }
        public string VoucherOnline { get; set; }
        public string StyleProfile { get; set; }
        public DateTime? LastDateModified { get; set; }
        public string LastDateModifiedStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }

    }
    public class CheckStoreListModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string Address { get; set; }
        public string TaxCode { get; set; }
        public string PhoneNo { get; set; }
        public DateTime? LastDateModified { get; set; }
        public string LastDateModifiedStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public bool Status { get; set; }
        public string Message { get; set; }
    }

    public class StoreListSpecialComboModel
    {
        public List<StorePartnerModel> StoreList { get; set; }
        public List<StorePartnerModel> StoreListSelected { get; set; }
    }



}
