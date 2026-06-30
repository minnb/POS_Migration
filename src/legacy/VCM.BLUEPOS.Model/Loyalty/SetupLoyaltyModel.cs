using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Loyalty
{
    public class GiftRedeemResponseModel
    {
        public int ID { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string SalesUnitOfMeasure { get; set; }
        public int PointRedeem { get; set; }
        public string StatusStr { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public string CreatedUser { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public string UpdatedDate { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string ClubCode { get; set; }
        public int Total { get; set; }

    }
    public class SetupLoyaltyResponseModel
    {
        public int ID { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string SalesUnitOfMeasure { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public int Total { get; set; }

    }

    public class ProductListForLoyaltyResponseModel
    {
        public int ID { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string SalesUnitOfMeasure { get; set; }

    }

    public class ItemGiftRedeemForTabGiftRedeemModel
    {  
        public string ItemNo { get; set; }
        public string PointRedeem { get; set; }
        public string ClubCode { get; set; } // hoi vien
        public string Status { get; set; }
        public string Pkey { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
    }

    public class ListGiftRedeemModalModel
    {
        public int ID { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string SalesUnitOfMeasure { get; set; }
        public int PointRedeem { get; set; }
        public string Status { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string ClubCode { get; set; } // hoi vien
        public int Total { get; set; }

    }

    public class CreateSetupLoyaltyModel
    {
        public int ID { get; set; }
        public List<ProductListForLoyaltyResponseModel> ListItem { get; set; }
        public string ItemNoStr { get; set; }
        public string ItemName { get; set; }
        public string SalesUnitOfMeasure { get; set; }
        public string ClubCode { get; set; }  // hoi vien
        public int Point { get; set; }
        public string PointStr { get; set; }
        public int Status { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedUser { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public DateTime UpdatedDate { get; set; }

    }

    public class CreateSetupLoyaltyTabGiftRedeemModel
    {
        public int ID { get; set; }
        public List<ItemGiftRedeemForTabGiftRedeemModel> ListItem { get; set; }
        public string ListItemStr { get; set; }
        public string ItemName { get; set; }
        public string SalesUnitOfMeasure { get; set; }
        public int? Point { get; set; }
        public string PointStr { get; set; }
        public int Status { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedUser { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string ClubCode { get; set; } // hoi vien

    }
    public class UpdateSetupLoyaltyModel
    {
        public string ID { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public int? Point { get; set; }
        public string PointStr { get; set; }
        public int Status { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedUser { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string ClubCode { get; set; } // hoi vien

    }

    public class DeleteLoyaltyModel
    {
        public string ID { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public int Point { get; set; }
        public string PointStr { get; set; }
        public int Status { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedUser { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public DateTime UpdatedDate { get; set; }

    }

    public class ExportExcelGiftRedeemResponseModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string SalesUnitOfMeasure { get; set; }
        public int PointRedeem { get; set; }
        public string StatusStr { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public string CreatedUser { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public string UpdatedDate { get; set; }
    }

    public class GiftRedeemModifyLogModel
    {
        public string ItemNo { get; set; }
        public int Point { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Status { get; set; }
        public string StoreNo { get; set; }
        public string POSNo { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedUser { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class ImportExcelGiftRedeemModel
    {
        public string ItemNo { get; set; }
        public int PointRedeem { get; set; }
        public string PointRedeemStr { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public string ClubCode { get; set; } // hoi vien
    }

    //--- 22/05/2024 ---

    public class MemberEarnItemResponseModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string Uom { get; set; }
        public string Size { get; set; }
        public int SaleQty { get; set; }
        public int StampsQty { get; set; }
        public string Blocked { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public string CreatedUser { get; set; }
        public string CreateDateStr { get; set; }
        public string UpdatedDateStr { get; set; }
        public int Total { get; set; }

    }

    public class ExportMemberEarnItemResponseModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string Uom { get; set; }
        public string Size { get; set; }
        public int SaleQty { get; set; }
        public int StampsQty { get; set; }
        public string Blocked { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public string CreateDateStr { get; set; }

    }

    public class DeleteSetupMemberEarnItemModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string Uom { get; set; }
        public string Size { get; set; }
        public int Blocked { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }

    }

    public class ImportExcelSetupMemberEarnItemModel
    {
        public string ItemNo { get; set; }
        public string Uom { get; set; }
        public string Size { get; set; }
        public int SaleQty { get; set; }
        public int StampsQty { get; set; }
        public int Blocked { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public string CrtUser { get; set; }
        public DateTime? CrtdDate { get; set; }
        public DateTime? UpdDate { get; set; }

    }

    public class UpdateSetupMemberEarnItemModel
    {
        public string ItemNo { get; set; }
        public string Uom { get; set; }
        public string Size { get; set; }
        public int SaleQty { get; set; }
        public int StampsQty { get; set; }
        public string Blocked { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public string CrtUser { get; set; }
        public DateTime? CrtdDate { get; set; }
        public DateTime? UpdDate { get; set; }

    }

    public class GiftCouponResponseModel
    {
        public string CrtDateStr { get; set; }
        public string OrderNo { get; set; }
        public string OrderNoUsed { get; set; }
        public string MemberCard { get; set; }  // dien thoai
        public string CouponCode { get; set; }  // so serial
        public string MaterialNo { get; set; }  // ma coupon
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public string IsUsed { get; set; }
        public string IsSync { get; set; }
        public string Message { get; set; }
        public int Total { get; set; }

    }

    public class ExportExcelGiftCouponResponseModel
    {
        public string CrtDateStr { get; set; }
        public string OrderNo { get; set; }
        public string MemberCard { get; set; }
        public string CouponCode { get; set; }   // so serial
        public string MaterialNo { get; set; }   // ma coupon
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public string IsUsed { get; set; }
        public string IsSync { get; set; }
        public string Message { get; set; }
        public string OrderNoUsed { get; set; }

    }




}
