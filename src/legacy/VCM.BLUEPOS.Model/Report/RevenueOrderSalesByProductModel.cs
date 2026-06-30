using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class RevenueOrderSalesByProductModel
    {
        public int ID { get; set; }
        public string OrderNo { get; set; }
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string OrderDateStr { get; set; }
        public string MCHCode1 { get; set; }                                // Mã ngành hàng cha
        public string MCHName1 { get; set; }                                // Ngành hàng cha
        public string MCHName2 { get; set; }                                // Ngành hàng con
        public string ItemNo { get; set; }                                  // Mã sản phẩm
        public string Description { get; set; }                             // Tên sản phẩm
        public string ItemNoPLG { get; set; }                               // Mã sản phẩm tham chiếu
        public double Quantity { get; set; }                                // Số lượng
        public double UnitPrice { get; set; }                               // Đơn giá
        public Nullable<double> AmountBeforeDiscount { get; set; }          // Tổng doanh thu trước khi giảm giá
        public double DiscountAmount { get; set; }                          // Giảm giá   
        public Nullable<double> PrepaymentAmountIncVAT { get; set; }        // Tổng doanh thu sau khi giảm giá
        public Nullable<double> VATAmount { get; set; }                     // Tiền thuế VAT
        public Nullable<double> TotalAmountBeforeDiscount { get; set; }      // Tổng cộng: doanh thu trước khi giảm giá
        public double TotalDiscountAmount { get; set; }                      // Tổng cộng: Giảm giá   
        public Nullable<double> TotalPrepaymentAmountIncVAT { get; set; }    // Tổng cộng: Tổng doanh thu sau khi giảm giá
        public Nullable<double> TotalVATAmount { get; set; }                 // Tổng cộng: Tiền thuế VAT
        public int Total { get; set; }

    }

    public class ExportExcelRevenueOrderSalesByProductModel
    {
        public string StoreNo { get; set; }
        public string StoreName { get; set; }
        public string OrderDateStr { get; set; }
        public string MCHName1 { get; set; }                                // Ngành hàng cha
        public string MCHName2 { get; set; }                                // Ngành hàng con
        public string OrderNo { get; set; }
        public string ItemNo { get; set; }                                  // Mã sản phẩm
        public string Description { get; set; }                             // Tên sản phẩm
        public string ItemNoPLG { get; set; }                               // Mã sản phẩm tham chiếu
        public double Quantity { get; set; }                                // Số lượng
        public double UnitPrice { get; set; }                               // Đơn giá
        public Nullable<double> AmountBeforeDiscount { get; set; }          // Tổng doanh thu trước khi giảm giá
        public double DiscountAmount { get; set; }                          // Giảm giá   
        public Nullable<double> PrepaymentAmountIncVAT { get; set; }        // Tổng doanh thu sau khi giảm giá
        public Nullable<double> VATAmount { get; set; }                     // Tiền thuế VAT
        public Nullable<double> TotalAmountBeforeDiscount { get; set; }     // Tổng cộng: doanh thu trước khi giảm giá
        public double TotalDiscountAmount { get; set; }                     // Tổng cộng: Giảm giá   
        public Nullable<double> TotalPrepaymentAmountIncVAT { get; set; }   // Tổng cộng: Tổng doanh thu sau khi giảm giá
        public Nullable<double> TotalVATAmount { get; set; }                // Tổng cộng: Tiền thuế VAT
    }


}
