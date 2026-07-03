using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Partner
{
    public class CreateItemPartnerModel
    {
        public string partner_id { get; set; }              // Đối tác
        public int restaurant_id { get; set; }
        public string partner_restaurant_id { get; set; }   // StoreNo
        public string partner_dish_id { get; set; }         // ItemNo
        public string name { get; set; }                    // Tên sản phẩm
        public string partner_dish_group_id { get; set; }   // Nhóm loại
        public Nullable<decimal> price { get; set; }        // Giá bán
        public string name_en { get; set; }
        public int display_order { get; set; }
        public string description { get; set; }             // Diễn giải thông tin sản phẩm
        public string picture_id { get; set; }
        public string picture_url { get; set; }
        public string merchant_item_image { get; set; }
    }

    public class APICreateItemNowFoodModel
    {
        public int restaurant_id { get; set; }
        public string partner_restaurant_id { get; set; }           // StoreNo
        public string partner_dish_id { get; set; }                 // ItemNo
        public string name { get; set; }                            // Tên sản phẩm
        public string partner_dish_group_id { get; set; }           // Nhóm loại
        public Nullable<decimal> price { get; set; }                // Giá bán
        public string name_en { get; set; }
        public int display_order { get; set; }
        public string description { get; set; }                     // Diễn giải thông tin sản phẩm
        public int picture_id { get; set; }        
    }

    public class UpdateItemListNowFoodModel
    {
        public string PartnerCode { get; set; }
        public string AppCode { get; set; }
        public string StoreNo { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string ItemGroup { get; set; }
        public string ItemGroupName { get; set; }
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
    public class PictureResponseModel
    {
        // now food
        public int picture_id { get; set; }
        public string url { get; set; }

        // grab food
        public string ItemId { get; set; }
        public string Photo { get; set; }

        // be food
        public int MerchantItemId { get; set; }
        public string MerchantItemImage { get; set; }
    }

    //-------- 25/12/2024 : BeFood

    public class ApiCreateItemByBeFoodModel
    {
        public int restaurant_id { get; set; }                  // StoreNo
        public string merchant_item_name { get; set; }          // Tên sản phẩm
        public string merchant_item_description { get; set; }   // Thông tin diễn giải
        public string merchant_item_image { get; set; }         // Hình ảnh
        public decimal? merchant_item_price { get; set; }       // Giá
        public string merchant_category_id { get; set; }        // Mã ngành hàng
        public string partner_category_id { get; set; }         //  Mã ngành hàng
        public string reference_id { get; set; }                // Mã Item PLH
    }



}
