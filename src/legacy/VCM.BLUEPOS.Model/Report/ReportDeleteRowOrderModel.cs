using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class ReportDeleteTotalRowOrderModel
    {
        public int ID { get; set; }
        public string OrderTimeStr { get; set; }
        public string StaffID { get; set; }
        public string StoreNo { get; set; }
        public int? TotalRow { get; set; }
    }

    public class ExportReportDeleteTotalRowOrderModel
    {
        public string StaffID { get; set; }
        public string StoreNo { get; set; }
        public int? TotalRow { get; set; }
    }

    public class ReportDetailDeleteRowOrderModel
    {
        public int ID { get; set; }
        public string StaffID { get; set; }
        public string StoreNo { get; set; }                 // Cửa hàng
        public string OrderNo { get; set; }                 // Mã đơn hàng
        public string DateTimeStr { get; set; }
        public string TimeStr { get; set; }                 // Thời gian
        public string DeleteTimeStr { get; set; }           // Ngày hủy
        public string ItemNo { get; set; }                  // Mã sản phẩm
        public string Description { get; set; }             // Tên sản phẩm
        public string Quantity { get; set; }
        public string UnitPrice { get; set; }
        public string DiscountAmount { get; set; }
        public string LineAmountIncVAT { get; set; }
        public int TotalDeleteRow { get; set; }            // Tong so dong huy theo nhan vien
        public int Total { get; set; }
    }

    public class ExportDetailDeleteRowOrderModel
    {
        public string StaffID { get; set; }             // Ma nhan vien
        public string StoreNo { get; set; }
        public string OrderNo { get; set; }             // Số đơn hàng
        public string DateTimeStr { get; set; }
        public string TimeStr { get; set; }
        public string DeleteTimeStr { get; set; }
        public string ItemNo { get; set; }              // Ma san pham
        public string Description { get; set; }         // Ten san pham
        public string Quantity { get; set; }            // So luong
        public string UnitPrice { get; set; }           // Don gia
        public string DiscountAmount { get; set; }      // Giam gia
        public string LineAmountIncVAT { get; set; }    // Thue VAT

    }

    public class ReportDeleteOrderListModel
    {
        public int ID { get; set; }
        public string StaffID { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminalNo { get; set; }
        public string OrderNo { get; set; }
        public string OrderType { get; set; }
        public string TotalDiscountAmount { get; set; }
        public string TotalAmount { get; set; }
        public string OrderTimeStr { get; set; }
        public string TimeStr { get; set; }
        public string DeleteTimeStr { get; set; }
        public int Total { get; set; }

    }

    public class ExportReportDeleteOrderListModel
    {
        public string StoreNo { get; set; }
        public string POSTerminalNo { get; set; }
        public string StaffID { get; set; }
        public string OrderNo { get; set; }
        public string OrderType { get; set; }
        public string OrderTimeStr { get; set; }
        public string DeleteTimeStr { get; set; }
        public string TotalDiscountAmount { get; set; }
        public string TotalAmount { get; set; }

    }

    public class DetailRevenueReportResponseModel
    {
        public int ID { get; set; }
        public string OrderNo { get; set; }                       // Số đơn hàng
        public string StartingTimeStr { get; set; }               // Ngay tao don hang
        public string OrderTimeStr { get; set; }                  // Thoi gian
        public string OrderDateStr { get; set; }                  // Ngay kinh doanh
        public string StoreNo { get; set; }                       // Ma ST/CH
        public string POSTerminalNo { get; set; }                 // May POS
        public string CashierID { get; set; }                     // Thu ngan
        public string OrigOrder { get; set; }                     // Đơn hàng gốc
        public string ReferenceNo { get; set; }                   // Đơn hàng tham chiếu
        public string OrderType { get; set; }                     // SalesIsReturn : 0 : DH ban, 1 : DH tra  
        public string ItemNo { get; set; }                        // Mã sản phẩm
        public string Description { get; set; }                   // Ten san pham
        public string UnitOfMeasure { get; set; }                 // Don vi tinh
        public string Size { get; set; }                          // Kich thuoc
        public string CupType { get; set; }                       // Loai ly
        public string PromotionID { get; set; }                   // Mã CTKM, Mã voucher
        public string CouponCode { get; set; }                    // Mã Coupon
        public string Barcode { get; set; }                       // Mã Barcode
        public double Quantity { get; set; }                      // Số lượng
        public double UnitPrice { get; set; }                     // Đơn giá
        public double DiscountAmount { get; set; }                // Giảm giá
        public Nullable<double> LineAmountBeforeVAT { get; set; } // Thành tiền trước thuế 
        public string VATCode { get; set; }                       // Tỷ lệ thuế suất % VAT
        public Nullable<double> VATAmount { get; set; }           // Tiền thuế VAT
        public Nullable<double> LineAmountIncVAT { get; set; }    // Thành tiền sau thuế VAT
        public string CardVinID { get; set; }                     // MemberCardNo / the VinID
        public double MemberPointsEarn { get; set; }
        public double MemberPointsRedeem { get; set; }
        public string BlockedMemberPoint { get; set; }
        public double AmountCalPoint { get; set; }
        public string TenderType { get; set; }
        public string TenderTypeName { get; set; }
        public string ApprovalCode { get; set; }            // Ma chuan chi
        public string BankPOSCode { get; set; }
        public string BankCardType { get; set; }
        public string IsOnline { get; set; }        
        public string MCHName { get; set; }                 // Ten nganh hang
        public string CouponType { get; set; }              // Loai Coupon
        public string Note { get; set; }                    // Ghi chu
        public string SalesTypeName { get; set; }           // Hinh thuc ban hang
        public string PromotionName { get; set; }           // Ten CTKM
        public string VoucherCouponName { get; set; }
        public string UserID { get; set; }                  // Nơi tạo bill
        public int Total { get; set; }

    }

    public class ExportDetailRevenueReportResponseModel
    {
        public string StartingTimeStr { get; set; }
        public string OrderTimeStr { get; set; }
        public string OrderDateStr { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminalNo { get; set; }
        public string OrderNo { get; set; }                                 // Số đơn hàng
        public string OrigOrder { get; set; }                               // Đơn hàng gốc
        public string OrderType { get; set; }                               // SalesIsReturn : 0 : DH ban, 1 : DH tra  
        public string PromotionID { get; set; }                             // Mã CTKM hoặc mã Voucher
        public string CouponCode { get; set; }                              // Mã Coupon
        public string Barcode { get; set; }                                 // Mã Barcode
        public string ItemNo { get; set; }                                  // Mã sản phẩm
        public string Description { get; set; }                             // Tên sản phẩm
        public string Size { get; set; }
        public string CupType { get; set; }
        public string UnitOfMeasure { get; set; }       
        public double Quantity { get; set; }                                // Số lượng
        public double UnitPrice { get; set; }                               // Đơn giá
        public double DiscountAmount { get; set; }                          // Giảm giá
        public string PromotionName { get; set; }
        public string VoucherCouponName { get; set; }
        public Nullable<double> LineAmountBeforeVAT { get; set; }           // Thành tiền trước thuế
        public string VATCode { get; set; }                                 // Tỷ lệ thuế suất %
        public string VATCodeStr { get; set; }                              // Tỷ lệ thue suat VAT %
        public Nullable<double> VATAmount { get; set; }                     // Tiền thuế VAT
        public Nullable<double> LineAmountIncVAT { get; set; }              // Thành tiền sau thuế VAT
        public string CardVinID { get; set; }                               // MemberCardNo = Số thẻ VinID
        public double MemberPointsEarn { get; set; }
        public double MemberPointsRedeem { get; set; }
        public double AmountCalPoint { get; set; }
        public string CashierID { get; set; }
        public string MCHName { get; set; }

    }

    public class ExportExcelDetailRevenueReportModel
    {
        public string StartingTimeStr { get; set; }
        public string OrderTimeStr { get; set; }
        public string OrderDateStr { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminalNo { get; set; }                   // Máy POS
        public string OrderNo { get; set; }                         // Số đơn hàng
        public string OrigOrder { get; set; }                       // Đơn hàng gốc
        public string OrderType { get; set; }                       // SalesIsReturn : 0 : DH ban, 1 : DH tra  
        public string SalesTypeName { get; set; }
        public string PromotionID { get; set; }                     // Mã CTKM hoặc mã Voucher
        public string CouponCode { get; set; }                      // Mã Coupon
        public string Barcode { get; set; }                         // Mã Barcode
        public string ItemNo { get; set; }                          // Mã sản phẩm
        public string Description { get; set; }                     // Tên sản phẩm
        public string Size { get; set; }
        public string CupType { get; set; }
        public string UnitOfMeasure { get; set; }                   // Đơn vị tính       
        public double Quantity { get; set; }                        // Số lượng
        public double UnitPrice { get; set; }                       // Đơn giá
        public double DiscountAmount { get; set; }                  // Giảm giá
        public string PromotionName { get; set; }
        public string VoucherCouponName { get; set; }
        public Nullable<double> LineAmountBeforeVAT { get; set; }   // Thành tiền trước thuế VAT
        public string VATCode { get; set; }                         // Tỷ lệ thuế suất %
        public string VATCodeStr { get; set; }                      // Thuế suất VAT
        public Nullable<double> VATAmount { get; set; }             // Tiền thuế VAT
        public Nullable<double> LineAmountIncVAT { get; set; }      // Thành tiền sau thuế VAT
        public string CardVinID { get; set; }                       // MemberCardNo
        public double MemberPointsEarn { get; set; }
        public double MemberPointsRedeem { get; set; }
        public double AmountCalPoint { get; set; }
        public string CashierID { get; set; }     
        public string MCHName { get; set; }
        public string CouponType { get; set; }
        public string Note { get; set; }
        public string UserID { get; set; }                          // Noi tao bill
        public string ReferenceNo { get; set; }                     // Đơn hàng tham chiếu

    }

    public class SalesDetailPromotionModel
    {
        public int ID { get; set; }
        public string OrderNo { get; set; }                       // Số đơn hàng
        public string OrderDateStr { get; set; }                  // Ngay kinh doanh
        public string StoreNo { get; set; }                       // Ma ST/CH
        public string POSTerminalNo { get; set; }                 // May POS
        public string TenderType { get; set; }
        public string SalesTypeName { get; set; }                 // Hinh thuc ban hang
        public string OrigOrder { get; set; }                     // Đơn hàng gốc
        public string OrderType { get; set; }                     // SalesIsReturn : 0 : DH ban, 1 : DH tra  
        public string Barcode { get; set; }                       // Mã Barcode
        public string ItemNo { get; set; }                        // Mã sản phẩm
        public string Description { get; set; }                   // Ten san pham
        public string PromotionNo { get; set; }                   // Mã CTKM, Mã voucher
        public string UnitOfMeasure { get; set; }                 // Don vi tinh
        public Nullable<double> Quantity { get; set; }                      // Số lượng
        public Nullable<double> UnitPrice { get; set; }                     // Đơn giá
        public Nullable<double> DiscountAmount { get; set; }                // Giảm giá
        public Nullable<double> LineAmountBeforeVAT { get; set; }           // Thành tiền trước thuế 
        public string VATCode { get; set; }                                 // Tỷ lệ thuế suất % VAT
        public Nullable<double> VATAmount { get; set; }                     // Tiền thuế VAT
        public Nullable<double> LineAmountIncVAT { get; set; }              // Thành tiền sau thuế VAT        
        public int Total { get; set; }

    }







}
