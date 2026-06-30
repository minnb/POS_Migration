using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.MasterData
{
    public class SetupImageSliderResponseModel
    {    
        public long? ID { get; set; }
        public string StoreNo { get; set; }
        public string POSNo { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public string FileName { get; set; }
        public string UrlFile { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedDate { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public int Total { get; set; }
        
    }

    public class CreateSetupImageSliderModel
    {
        public long? ID { get; set; }
        public string StoreNo { get; set; }
        public string POSNo { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public string FileName { get; set; }
        public string UrlFile { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedDate { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public string ImageName { get; set; }
        public string ImageBase64 { get; set; }
        public List<StoreListModel> StoreList { get; set; }
        public string StoreListStr { get; set; }
        public bool? IsSelectAll { get; set; }
        public List<StoreListModel> StoreList2 { get; set; }
        public string StoreListStr2 { get; set; }
        public bool? IsSelectAll2 { get; set; }
        public bool? CheckStore { get; set; }
    }

    public class StoreListModel
    {
        public string StoreNo { get; set; }
    }

    public class UpdateSetupImageSliderModel
    {
        public long? ID { get; set; }
        public string StoreNo { get; set; }
        public string POSNo { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FileName { get; set; }
        public string FileNameNew { get; set; }
        public string FileNameOld { get; set; }
        public string UrlFile { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedDate { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public string ImageName { get; set; }
        public string ImageBase64 { get; set; }
    }
    //07/08/2025,tungnt8
    public class ImageSliderBlockModel
    { 
        public string Description { get; set; }
        public string Status { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedDate { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class DeleteSetupImageSliderModel
    {
        public long? ID { get; set; }
        public string Pkey { get; set; }
        public string StoreNo { get; set; }
        public string POSNo { get; set; }
    }

    public class ExportExcel_ImagesSliderListResponseModel
    {
        public long? ID { get; set; }
        public string StoreNo { get; set; }
        public string POSNo { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string FileName { get; set; }
        public string UrlFile { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Pkey { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedDate { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }

    }

    public class SetupCurrencyRateListModel
    {
        public int ID { get; set; }
        public string CurrencyCode { get; set; }
        public string StartingDate { get; set; }
        public string EndingDate { get; set; }
        public Nullable<double> ExchangeRate { get; set; }
        public string Status { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public int Total { get; set; }

    }
    public class UserAdminLoginPOSResponseModel
    {
        public long? ID { get; set; }
        public string StoreNo { get; set; }
        public string POSNo { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string FileName { get; set; }
        public string UrlFile { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedDate { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public int Total { get; set; }

    }

    // Table: AdminUserLoginPOS
    public class AdminUserLoginPOSResponseModel
    {
        public int ID { get; set; }
        public string UserName { get; set; }
        public string StaffCode { get; set; }
        public string FullName { get; set; }
        public string StoreNo { get; set; }
        public string PosNo { get; set; }
        public string Token { get; set; }
        public string TypePOS { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedDateStr { get; set; }
        public int Total { get; set; }

    }

    public class ExportAdminUserLoginPOSResponseModel
    {
        public string UserName { get; set; }
        public string StaffCode { get; set; }
        public string FullName { get; set; }
        public string StoreNo { get; set; }
        public string PosNo { get; set; }
        public string TypePOS { get; set; }
        public string Token { get; set; }
        public string CreatedDateStr { get; set; }
    }


    public class DeleteAdminUserLoginPOSWebModel
    {
        public int ID { get; set; }
        public string UserName { get; set; }
        public string StaffCode { get; set; }
        public string StoreNo { get; set; }
        public string PosNo { get; set; }        
    }




}
