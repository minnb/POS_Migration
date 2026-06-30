using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report.PromotionOfferTypeComboModel
{
    public class PromotionOfferTypeComboResponseModel
    {
        public int ID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string StyleProfile { get; set; }
        public string SetDB { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string OfferType { get; set; }               // Loại CTKM combo
        public string OfferNo { get; set; }                 // Mã CTKM Combo
        public string OfferName { get; set; }               // Tên CTKM combo
        public string BussinessDate { get; set; }           // Ngày kinh doanh
        public double? SL_Combo { get; set; }                  // Số lượng Combo
        public double? TotalAmount { get; set; }            // Tổng số tiền giảm giá theo combo
        public int? Total { get; set; }
    }

    public class DetailPromotionOfferTypeComboResponseModel
    {
        public int ID { get; set; }
        public string OrderNo { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string StyleProfile { get; set; }
        public string SetDB { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string OfferType { get; set; }               // Loại CTKM combo
        public string OfferNo { get; set; }                 // Mã CTKM Combo
        public string OfferName { get; set; }               // Tên CTKM combo
        public string BussinessDate { get; set; }           // Ngày kinh doanh

        public string ItemNo { get; set; }                  // Mã sản phẩm
        public string Description { get; set; }             // Tên sản phẩm
        public double? Quantity { get; set; }               // Số lượng
        public double? UnitPrice { get; set; }              // Giá bán
        public double? DiscountAmount { get; set; }         // Tiền giảm giá
        public double? PrepaymentAmount { get; set; }       // Thành tiền bao gồm VAT

        public int? Total { get; set; }
    }



}
