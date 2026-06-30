using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Partner
{
    public class OrderSalesListByNowFoodModel
    {
        public int ID { get; set; }
        public string order_code { get; set; }
        public int update_type { get; set; }
        public string restaurant_id { get; set; }
        public string pick_time_Str { get; set; }
        public string StatusStr { get; set; }         // Trang thai don hang
        public string merchant_note { get; set; }
        public string note_for_shipper { get; set; }
        public string partner_restaurant_id { get; set; }
        public string serial { get; set; }
        public string crt_date_Str { get; set; }
        public string chg_date { get; set; }
        public string update_flg { get; set; }      // Trang thai thanh toan
        public string message { get; set; }
        public string crt_user { get; set; }
        public string raw_data { get; set; }
        public int Total { get; set; }
    }

    public class OrderSalesListByNowFoodResponseModel
    {
        public int ID { get; set; }
        public string order_code { get; set; }
        public int update_type { get; set; }
        public int restaurant_id { get; set; }
        public string pick_time { get; set; }
        public int status { get; set; }
        public string merchant_note { get; set; }
        public string note_for_shipper { get; set; }
        public string partner_restaurant_id { get; set; }
        public string serial { get; set; }
        public string crt_date { get; set; }
        public string chg_date { get; set; }
        public string update_flg { get; set; }
        public string message { get; set; }
        public string crt_user { get; set; }
        public string raw_data { get; set; }
        public int Total { get; set; }
    }

    public class ExportExcel_OrderSalesListByNowFoodModel
    {
        public string crt_date_Str { get; set; }
        public string pick_time_Str { get; set; }
        public string order_code { get; set; }
        public string restaurant_id { get; set; }
        public string partner_restaurant_id { get; set; }
        public string StatusStr { get; set; }
        public string update_flg { get; set; }
        public string note_for_shipper { get; set; }
    }

}
