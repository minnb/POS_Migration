using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Campaign
{
    public class CampaignCreateModel
    {
        public string MerchantID { get; set; }            // BEFOOD, GRABFOOD
        public string Name { get; set; }                  // Tên món hiển thị trên app
        public string Description { get; set; }           // Diễn giải cho món
        public string Photo { get; set; }                 // Ảnh hiển thị trên app
        public string StartTime { get; set; }             // yyyy-MM-dd HH:mm:ss
        public string EndTime { get; set; }               // yyyy-MM-dd HH:mm:ss
        public string OfferNo { get; set; }               // Mã offerNo SAP nếu có
        public string OfferType { get; set; }             // ZB02, ZB07
        public int Price { get; set; }                    // Tổng tiền combo
        public int Value { get; set; }                    // Số tiền giảm giá combo
        public string CrtUser { get; set; }               // User tạo
        public List<string> StoreNo { get; set; }         // Null: áp dụng all store, hoặc danh sách store

        public List<CampaignItemCreateModel> CampaignItems { get; set; }
    }

    public class CampaignItemCreateModel
    {
        public int Sequence { get; set; }                 // Thứ tự
        public List<string> Items { get; set; }           // Danh sách mã sản phẩm
        public int SpecialPrice { get; set; }             // Giá bán món trong combo
        public int Discount { get; set; }                 // Giảm giá món trong combo
        public bool IsTopping { get; set; }               // Có topping không
    }
}
