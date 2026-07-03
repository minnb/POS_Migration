using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion.SetupSpecialComboModel
{
    public class GetSpecialComboHeaderModel
    {
        public string Code { get; set; }
        public string SalesType { get; set; }
        public string Name { get; set; }
        public Nullable<double> ComboQuantity { get; set; }
        public Nullable<double> Amount { get; set; }
        public string IsMember { get; set; }    
        public string MemberCode { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string Status { get; set; }
        public string Pkey { get; set; }
        public long? Counter { get; set; }
        public string CreatedDateStr { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedDateStr { get; set; }
        public string UpdatedBy { get; set; }
        public int Total { get; set; }
    }

    public class DetailSpecialComboModel
    {
        public string StoreNo { get; set; }
        public string Code { get; set; }       
        public string Name { get; set; }
        public string SalesType { get; set; }
        public Nullable<double> ComboQuantity { get; set; }
        public Nullable<double> Amount { get; set; }
        public string IsMember { get; set; }
        public string MemberCode { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string Status { get; set; }
        public int Order { get; set; }
        public string GroupCode { get; set; }
        public string GroupName { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UOM { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public Nullable<double> MinQuantity { get; set; }
        public Nullable<double> MaxQuantity { get; set; }
        public string IsDefault { get; set; }
        public string IsDynamicPrice { get; set; }
        public string IsDynamicPriceStr { get; set; }
        public string IsSendSAP { get; set; }
        public int Total { get; set; }
    }

    public class CreateSetupSpecialComboModel
    {
        // table : SpecialComboHeader
        public List<string> StoreNo { get; set; }
        public string Code { get; set; }
        public string SalesType { get; set; }
        public string Name { get; set; }
        public string ComboQuantity { get; set; }
        public string Amount { get; set; }
        public string IsMember { get; set; }
        public string MemberCode { get; set; }
        public Nullable<System.DateTime> FromDate { get; set; }
        public Nullable<System.DateTime> ToDate { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public string IsEnable { get; set; }
        public string Pkey { get; set; }
        public long? Counter { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string StatusStore { get; set; }

        // table : SpecialComboLine
        public string Line_Code { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UOM { get; set; }
        public string GroupCode { get; set; }
        public string GroupName { get; set; }
        public string IsRequired { get; set; }
        public string IsDefault { get; set; }
        public string IsSendSAP { get; set; }
        public string MinQuantity { get; set; }
        public string MaxQuantity { get; set; }
        public string Order { get; set; }
        public string IsEnableLine { get; set; }
        public string IsDynamicPrice { get; set; }
        public string IsDynamicPriceStr { get; set; }
        public List<CreateSetupSpecialComboLineModel> ListComboLine { get; set; }
    }

    public class CreateSetupSpecialComboLineModel
    {
        public string Code { get; set; }
        public string GroupCode { get; set; }
        public string GroupName { get; set; }
        public string MinimumQuantity { get; set; }
        public string MaximumQuantity { get; set; }
        public string Order { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UOM { get; set; }
        public double? UnitPrice { get; set; }
        public string IsRequired { get; set; }
        public string IsDefault { get; set; }
        public string IsSendSAP { get; set; }
        public string IsEnable { get; set; }
        public string IsDynamicPrice { get; set; }
        public string IsDynamicPriceStr { get; set; }
        public string Pkey { get; set; }
        public string Counter { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class ItemListBySepcialComboLineModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UOM { get; set; }
        public string IsDefault { get; set; }
        public string IsDynamicPrice { get; set; }
    }

    public class UpdateSetupSpecialComboModel
    {
        // table : SpecialComboHeader
        public List<string> StoreNo { get; set; }
        public string StoreNoStr { get; set; }
        public string Code { get; set; }
        public string SalesType { get; set; }
        public string Name { get; set; }
        public string ComboQuantity { get; set; }
        public string Amount { get; set; }
        public string IsMember { get; set; }
        public string MemberCode { get; set; }
        public Nullable<System.DateTime> FromDate { get; set; }
        public Nullable<System.DateTime> ToDate { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public string IsEnable { get; set; }
        public string Pkey { get; set; }
        public long? Counter { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string StatusStore { get; set; }
        public string CheckOptionStore { get; set; }  // dùng để check khi thêm Cửa hàng theo cách thủ công nhập danh sách Store

        // table : SpecialComboLine
        public string Line_Code { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UOM { get; set; }
        public string GroupCode { get; set; }
        public string GroupName { get; set; }
        public string IsRequired { get; set; }
        public string IsDefault { get; set; }
        public string IsSendSAP { get; set; }
        public string MinQuantity { get; set; }
        public string MaxQuantity { get; set; }
        public string Order { get; set; }
        public string IsEnableLine { get; set; }
        public string IsDynamicPrice { get; set; }
        public string IsDynamicPriceStr { get; set; }
        public List<CreateSetupSpecialComboLineModel> ListComboLine { get; set; }
    }

    public class UpdateItemNoByGroupModel
    {
        // table : SpecialComboLine
        public string Code { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UOM { get; set; }
        public string GroupCode { get; set; }
        public string GroupName { get; set; }
        public string IsRequired { get; set; }
        public string IsDefault { get; set; }
        public string IsSendSAP { get; set; }
        public string MinQuantity { get; set; }
        public string MaxQuantity { get; set; }
        public string Order { get; set; }
        public string IsEnable { get; set; }
        public string IsDynamicPrice { get; set; }
        public string Pkey { get; set; }
        public long? Counter { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
    }

    public class DeleteItemNoByGroupModel
    {
        public string Code { get; set; }
        public string GroupCode { get; set; }
        public string ItemNo { get; set; }
        public string UOM { get; set; }
    }
    public class SpecialComboStoreModel
    {
        public string Code { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string Status { get; set; }     
        public string CreatedDateStr { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedDateStr { get; set; }
        public string UpdatedBy { get; set; }
        public int Total { get; set; }
    }




}
