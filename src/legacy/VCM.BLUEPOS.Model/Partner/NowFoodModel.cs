using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Partner
{
    public class ShopeeRestaurantResponseModel
    {
        public int restaurant_id { get; set; }
        public string partner_restaurant_id { get; set; }
        public string name { get; set; }
        public string city { get; set; }
        public string address { get; set; }            
        public string foody_service { get; set; }       
        public DateTime? crt_date { get; set; }
        public string crt_date_str { get; set; }
        public string is_header { get; set; }
        public string parent_restaurant { get; set; }
        public int Total { get; set; }
    }

    public class CreateShopeeRestaurantModel
    {
        public int restaurant_id { get; set; }
        public string partner_restaurant_id { get; set; }
        public string name { get; set; }
        public string city { get; set; }
        public string address { get; set; }
        public string foody_service { get; set; }
        public DateTime? crt_date { get; set; }
        public string is_header { get; set; }
        public string parent_restaurant { get; set; }        
    }

    public class UpdateShopeeRestaurantModel
    {
        public int restaurant_id { get; set; }
        public string partner_restaurant_id { get; set; }
        public string partner_restaurant_id_old { get; set; }
        public string name { get; set; }
        public string city { get; set; }
        public string address { get; set; }
        public string foody_service { get; set; }
        public DateTime? crt_date { get; set; }
        public string is_header { get; set; }
        public string parent_restaurant { get; set; }
    }

    public class StorePLHModel
    {
        public string No { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string PostCode { get; set; }
        public string LocationCode { get; set; }
    }

    public class ItemSalesOnAppModel
    {
        public string StoreNo { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string CupType { get; set; }
        public string Size { get; set; }
        public string Uom { get; set; }
        public Nullable<decimal> UnitPrice { get; set; }
        public string Blocked { get; set; }
        public string IsSync { get; set; }
        public string IsApplyAll { get; set; }
        public string SyncResults { get; set; }
        public string IsTopping { get; set; }
        public string IsSales { get; set; }
        public string AppCode { get; set; }
        public string CrtDate { get; set; }
        public string ChgeDate { get; set; }       
        public int Total { get; set; }
    }

    public class ExportItemSalesOnAppModel
    {
        public string StoreNo { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string CupType { get; set; }
        public string Size { get; set; }
        public string Uom { get; set; }
        public Nullable<decimal> UnitPrice { get; set; }
        public string Blocked { get; set; }
        public string IsSync { get; set; }
        public string IsApplyAll { get; set; }
        public string SyncResults { get; set; }
        public string IsTopping { get; set; }
        public string IsSales { get; set; }
        public string AppCode { get; set; }
        public string CrtDate { get; set; }
        public string ChgeDate { get; set; }
    }

    public class UpdateStatusModel
    {
        public string StoreNo { get; set; }
        public string OrderNo { get; set; }
        public string Status { get; set; }
        public string UrserName { get; set; }
        public DateTime? UpdateCreated { get; set; }
    }

    //----- GrabFood ----

    public class GrabFood_Store_Model
    {
        public string GrabMerchantID { get; set; }
        public string PartnerMerchantID { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string IsOpen { get; set; }
        public string DeliveryStartTime { get; set; }
        public string DeliveryEndTime { get; set; }
        public string CrtUser { get; set; }
        public string CrtDate { get; set; }
        public string ChgUser { get; set; }
        public string ChgDate { get; set; }
        public int Total { get; set; }
    }

    public class ImportStoreGrabFoodModel
    {
        public string GrabMerchantID { get; set; }
        public string PartnerMerchantID { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public byte IsOpen { get; set; }
        public string CloseReason { get; set; }
        public string DeliveryStartTime { get; set; }
        public string DeliveryEndTime { get; set; }
        public string SpecialStartTime { get; set; }
        public string SpecialEndTime { get; set; }
        public string DineInStartTime { get; set; }
        public string DineInEndTime { get; set; }
        public string UserName { get; set; }

    }

}
