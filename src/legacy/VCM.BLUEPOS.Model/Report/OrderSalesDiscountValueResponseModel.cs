using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class OrderSalesDiscountValueResponseModel
    {
        public int ID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string StyleProfile { get; set; }
        public string SetDB { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string OfferType { get; set; }               // Loại CTKM giảm giá
        public string OfferNo { get; set; }                 // Mã CTKM
        public string BussinessDate { get; set; }
        public int? CountOrderTotal { get; set; }           // Tổng số hóa đơn
        public double? TotalDiscountAmount { get; set; }    // Tổng tiền giảm giá
        public int? Total { get; set; }

        //public double? TruocVAT { get; set; }
        //public double? ThueVAT { get; set; }
        //public double? SumTruocVAT { get; set; }
        //public double? SumSauVAT { get; set; }
        //public double? SumVAT { get; set; }
        //public double? SumTotalAmount { get; set; }
        //public double? SumTotalOrder { get; set; }
    }

    public class DetailOrderSalesDiscountValueResponseModel
    {
        public int ID { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string StyleProfile { get; set; }
        public string SetDB { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string OfferType { get; set; }       // Loại CTKM giảm giá
        public string OfferNo { get; set; }         // Mã CTKM giảm giá
        public string BussinessDate { get; set; }  
        public double? DiscountPercent { get; set; }
        public double? DiscountAmount { get; set; }
        public double? DiscountLoyalty { get; set; }
        public double? TotalDiscountAmount { get; set; }  
        public double? SUM_DISCOUNT_PERCENT { get; set; }
        public double? SUM_DISCOUNT_AMOUNT { get; set; }
        public double? SUM_DISCOUNT_LOYALTY { get; set; }
        public double? SUM_TOTAL_DISCOUNT_AMOUNT { get; set; }
        public int? Total { get; set; }
        public string OrderNo { get; set; }
        public string CashierID { get; set; }
        public string ProName { get; set; }

        //public string DiscountPercent { get; set; }
        //public string DiscountAmount { get; set; }
        //public string DiscountLoyalty { get; set; }


    }



}
