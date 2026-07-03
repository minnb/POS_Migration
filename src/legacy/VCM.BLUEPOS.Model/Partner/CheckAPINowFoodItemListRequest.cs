using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Partner
{   
    public class ResponseModel
    {
        public int code { get; set; }
        public string message { get; set; }
    }

    public class APINowFoodResponseModel
    {
        public ResponseModel meta { get; set; }
        public APICreateItemNowFoodResponseModel data { get; set; }
    }

    public class APICreateItemNowFoodResponseModel
    {
        public string dish_id { get; set; }
        public bool is_pending { get; set; }
    }

    public class ApiCreateItemBeFoodResponseModel
    {
        public string dish_id { get; set; }
        public bool is_pending { get; set; }
    }

    public class ApiBeFoodResponseModel
    {
        public ResponseModel meta { get; set; }
        public List<ApiCreateItemBeFoodResponseModel> data { get; set; }
    }
    public class APIListItemNowFoodResponseModel
    {
        public ResponseModel meta { get; set; }
        public List<APINowFoodItemListRequest> data { get; set; }
    }

    public class APINowFoodItemListRequest
    {
        public string dish_id { get; set; }
        public string dish_group_id { get; set; }
        public string partner_dish_id { get; set; }
        public string name { get; set; }
        public string name_en { get; set; }
        public string display_order { get; set; }
        public double price { get; set; }
        public string description { get; set; }
        public bool is_active { get; set; }
        public string partner_dish_group_id { get; set; }
        public DateTime? created_time { get; set; }
        public DateTime? updated_time { get; set; }
        public PictuerRequest picture { get; set; }
    }

    public class PictuerRequest
    {
        public int id { get; set; }
        public string url { get; set; }
    }

    // ------ Update ------
    public class ApiListItemNowFoodByUpdateResponseModel
    {
        public ResponseModel meta { get; set; }
        public List<ApiNowFoodItemListByUpdateRequest> data { get; set; }
    }

    public class ApiNowFoodItemListByUpdateRequest
    {
        public string dish_id { get; set; }                 // Mã sản phẩm trên NowFood
        public string dish_group_id { get; set; }
        public string partner_dish_id { get; set; }         // Mã SAP
        public string name { get; set; }                    // Tên sản phẩm
        public string name_en { get; set; }
        public string display_order { get; set; }
        public double price { get; set; }                   // Giá
        public string description { get; set; }
        public bool is_active { get; set; }                 // Trạng thái khóa món : 1 = Active , 3 = Deactive
        public string partner_dish_group_id { get; set; }   // Nganh hang
        public DateTime? created_time { get; set; }
        public DateTime? updated_time { get; set; }
        public PictuerRequest picture { get; set; }
    }

    public class NowFoodItemListModel
    {
        public string dish_id { get; set; }                 // Mã sản phẩm trên NowFood
        public string dish_group_id { get; set; }
        public string partner_dish_id { get; set; }         // Mã SAP
        public string name { get; set; }                    // Tên sản phẩm
        public string name_en { get; set; }
        public string display_order { get; set; }
        public double price { get; set; }                   // Giá
        public string description { get; set; }
        public bool is_active { get; set; }                 // Trạng thái khóa món : 1 = Active , 3 = Deactive
        public string partner_dish_group_id { get; set; }   // Nganh hang
        public DateTime? created_time { get; set; }
        public DateTime? updated_time { get; set; }
        public PictuerRequest picture { get; set; }
    }

    // API: Grabfood
    public class APIItemGrabFoodByUpdateResponseModel
    {
        public ResponseModel meta { get; set; }
        public List<APIGrabFoodItemByUpdateRequest> data { get; set; }
    }

    public class APIGrabFoodItemByUpdateRequest
    {
        public string dish_id { get; set; }                 // Mã sản phẩm trên GrabFood
        public string dish_group_id { get; set; }
        public string partner_dish_id { get; set; }         // Mã SAP
        public string name { get; set; }                    // Tên sản phẩm
        public string name_en { get; set; }
        public string display_order { get; set; }
        public double price { get; set; }                   // Giá
        public string description { get; set; }
        public bool is_active { get; set; }                 // Trạng thái khóa món : 1 = Active , 3 = Deactive
        public string partner_dish_group_id { get; set; }   // Nganh hang
        public DateTime? created_time { get; set; }
        public DateTime? updated_time { get; set; }
        public PictuerRequest picture { get; set; }
    }

    public class GrabFoodItemListModel
    {
        public string dish_id { get; set; }                 // Mã sản phẩm trên GrabFood
        public string dish_group_id { get; set; }
        public string partner_dish_id { get; set; }         // Mã SAP
        public string name { get; set; }                    // Tên sản phẩm
        public string name_en { get; set; }
        public string display_order { get; set; }
        public double price { get; set; }                   // Giá
        public string description { get; set; }
        public bool is_active { get; set; }                 // Trạng thái khóa món : 1 = Active , 3 = Deactive
        public string partner_dish_group_id { get; set; }   // Nganh hang
        public DateTime? created_time { get; set; }
        public DateTime? updated_time { get; set; }
        public PictuerRequest picture { get; set; }
    }

}
