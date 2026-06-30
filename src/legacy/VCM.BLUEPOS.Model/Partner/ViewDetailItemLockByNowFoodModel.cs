using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Partner
{
    public class API_ItemLockNowFoodResponseModel
    {
        public LockResponseModel meta { get; set; }
        public SetupLockItemByNowFoodModel data { get; set; }
    }

    public class LockResponseModel
    {
        public int code { get; set; }
        public string message { get; set; }
    }

    // ---- Khóa món NOWFOOD ----

    public class SetupLockItemByNowFoodModel
    {
        public int restaurant_id { get; set; }              // Mã Store
        public string partner_restaurant_id { get; set; }   // Mã Store Header        
        public string partner_dish_id { get; set; }         // Mã SAP
        public bool? is_apply_all { get; set; }
        public DishesResponseModel dishes { get; set; }
        public int isActive { get; set; }
        public string dish_id { get; set; }                 // ItemNo

    }

    public class CreateLockItemByNowFoodModel
    {
        public int restaurant_id { get; set; }              // Mã Store
        public string partner_restaurant_id { get; set; }   // Mã Store Header        
        public bool? is_apply_all { get; set; }
        public List<DishesResponseModel> dishes { get; set; }
    }
    public class DishesResponseModel
    {
        public string partner_dish_id { get; set; }         // Mã SAP
        public int status { get; set; }
    }

    public class Api_CreateLockItemByNowFoodResponseModel
    {
        public LockResponseModel meta { get; set; }
        public DishesResponseModel data { get; set; }
        
    }

    // -------- update --------
    public class UpdateItemNoListByNowFoodModel
    {
        public int restaurant_id { get; set; }              // Mã Store trên NowFood
        public string partner_restaurant_id { get; set; }   // Mã Store Header        
        public string partner_dish_id { get; set; }         // Mã sản phẩm SAP
        public string name { get; set; }                    // Tên sản phẩm
        public string partner_dish_group_id { get; set; }   // Mã ngành hàng
        public decimal price { get; set; }                  // Giá
        public string name_en { get; set; }
        public int display_order { get; set; }
        public string description { get; set; }
        public int picture_id { get; set; }

    }

    public class UpdateItemNoByNowFoodModel
    {
        public int restaurant_id { get; set; }              // Mã Store
        public string partner_restaurant_id { get; set; }   // Mã Store Header        
        public bool? is_apply_all { get; set; }
        public List<Update_DishesResponseModel> dishes { get; set; }
    }

    public class Api_UpdateItemByNowFoodResponseModel
    {
        public LockResponseModel meta { get; set; }
        public DishesResponseModel data { get; set; }

    }

    public class Update_DishesResponseModel
    {
        public string partner_dish_id { get; set; }         // Mã SAP
        public int status { get; set; }
    }



}
