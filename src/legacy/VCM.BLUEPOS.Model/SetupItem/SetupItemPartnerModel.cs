using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupItem
{
    public class OptionModel
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }
    public class SetupItemPartnerModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public List<ItmeListPartnerModel> ListItemPartner { get; set; }
        public string ListItemPartnerStr { get; set; }      
        public string ImgName { get; set; }
        public string Images { get; set; }
        public string ItemDecs { get; set; }
        public string ItemType { get; set; }
        public string UOM { get; set; }
        public string TAX { get; set; }
        public string Size { get; set; }
        public string ParentCode { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsVAT { get; set; }
        public bool CheckManual { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string CupType { get; set; }
    }

    public class ItmeListPartnerModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UOM { get; set; }
        public string CategoryCode { get; set; }
        public string CategoryName { get; set; }
        public string Barcode { get; set; }
        public string SalesType { get; set; }
        public bool? CheckItem { get; set; }
        public string CupType { get; set; }
        public int dish_id { get; set; }
        public string ImgName { get; set; }
        public string Images { get; set; }
        public string Url { get; set; }

    }
    public class ViewSetupItemPartnerDetailModel
    {
        public string ItemNo { get; set; }
        public ItemPartnerModel InforItem { get; set; }
    }

    public class ItemPartnerModel
    {
        public string ItemGroup { get; set; }
        public string ItemGroupName { get; set; }
        public string StoreNo { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string SalesType { get; set; }
        public string Uom { get; set; }
        public Nullable<decimal> UnitPrice { get; set; }
        public string Size { get; set; }
        public string Blocked { get; set; }
        public bool? IsTopping { get; set; }
        public string CupType { get; set; }
        public int dish_id { get; set; }
        public string ImgName { get; set; }
        public string Images { get; set; }
        public int PictureId { get; set; }
        public string Url { get; set; }
        
    }
    public class UpdateItemPartnerModel
    {
        public string ItemGroup { get; set; }
        public string ItemGroupName { get; set; }
        public string StoreNo { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string SalesType { get; set; }
        public string Uom { get; set; }
        public decimal UnitPrice { get; set; }
        public string Size { get; set; }
        public string Blocked { get; set; }
        public bool IsTopping { get; set; }
        public bool IsActive { get; set; }
        public string CupType { get; set; }
        public int dish_id { get; set; }
        public string ImgName { get; set; }
        public string Images { get; set; }
        public string Url { get; set; }
    }

    public class OptionComboxModel
    {
        public string Value { get; set; }
        public string Text { get; set; }
        public int Order { get; set; }
    }


}
