using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Order
{
    public class OrderListModel
    {
        public int ID { get; set; }
        public string OrderNo { get; set; }
        public string StartingTimeStr { get; set; }
        public string OrderTimeStr { get; set; }
        public string OrderDateStr { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminalNo { get; set; }
        public string CashierID { get; set; }
        public double? TotalAmount { get; set; }        // AmountInclVAT
        public string OrigOrder { get; set; }           // Don hang goc
        public string OrderType { get; set; }           // SalesIsReturn : 0 : DH ban, 1 : DH tra  
        public string CustomerName { get; set; }        //
        public string SNG { get; set; }                 // Don hang Scan and go
        public string WMT { get; set; }                 // Don hang Online
        public string DeliveryComment { get; set; }     // DH giao tai nha
        public string CardVinID { get; set; }           // MemberCardNo
        public string ReturnVoucherNo { get; set; }     // Biên nhận thanh toán = BNTT
        public int Total { get; set; }

    }

    public class OrderListResponseModel
    {
        public int ID { get; set; }
        public string OrderNo { get; set; }                 // Số đơn hàng
        public string StartingTimeStr { get; set; }         // Ngày tạo đơn hàng
        public string OrderTimeStr { get; set; }            // Thời gian
        public string OrderDateStr { get; set; }            // Ngày kinh doanh
        public string StoreNo { get; set; }                 // Mã ST/CH
        public string POSTerminalNo { get; set; }           // Máy POS
        public string CashierID { get; set; }               // Thu ngân
        public double? TotalAmount { get; set; }            // Tổng số tiền
        public double? DiscountTotalAmount { get; set; }    // Giảm giá tổng bill
        public double? PaymentAtPOSAmount { get; set; }     // Thành tiền
        public string OrigOrder { get; set; }               // Đơn hàng gốc
        public string ReferenceNo { get; set; }             // Đơn hàng tham chieu
        public string OrderType { get; set; }               // SalesIsReturn : 0 : DH bán, 1 : DH trả 
        public string CustomerName { get; set; }            // Tên khách hàng
        public string SalesTypeCode { get; set; }           // Mã hình thức bán hàng ( ko duoc xoa, lay len de phan quyen button cap nhat sale type )
        public string SalesType { get; set; }               // Hình thức bán hàng
        public string CardCrownX { get; set; }              // Thẻ CrownX
        public string ReturnVoucherNo { get; set; }         // Biên nhận thanh toán = BNTT
        public string LabelStr { get; set; }
        public string Note { get; set; }
        public string UserID { get; set; }                  // Noi tao bill
        public int Total { get; set; }
        //public int PrintedNumber { get; set; }            // So lan in bill

    }

    public class ExportOrderListResponseModel
    {
        public string OrderNo { get; set; }
        public string StartingTimeStr { get; set; }
        public string OrderTimeStr { get; set; }
        public string OrderDateStr { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminalNo { get; set; }       
        public double? TotalAmount { get; set; }            // AmountInclVAT
        public double? DiscountTotalAmount { get; set; }    // Giảm giá tổng bill
        public double? PaymentAtPOSAmount { get; set; }     // Thành tiền
        public string OrigOrder { get; set; }               // Don hang goc
        public string ReferenceNo { get; set; }             // Đơn hàng tham chieu
        public string OrderType { get; set; }               // SalesIsReturn : 0 : DH ban, 1 : DH tra  
        public string CustomerName { get; set; } 
        public string SalesType { get; set; }               // Hình thuc ban hang
        public string CashierID { get; set; }
        public string LabelStr{ get; set; }
        public string UserID { get; set; }                  // Noi tao bill
        public string Note { get; set; }
        //public int PrintedNumber { get; set; }           // So lan in bill
    }

    public class DetailOrderListResponseModel
    {
        public string OrderNo { get; set; }
        public int LineNo { get; set; }
        public string ItemNo { get; set; }
        public string ItemNoPLG { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public string Barcode { get; set; }
        public double Quantity { get; set; }              
        public double UnitPrice { get; set; }           
        public double DiscountAmount { get; set; }
        public double LineAmountIncVAT { get; set; }        // Thành tiền
        public double VATAmount { get; set; }               // Thuế
        public string ToppingStr { get; set; }
        public double? MemberPointsEarnCVL { get; set; }    // Điểm tích
        public double? MemberPointsRedeemCVL { get; set; }  // Điểm tiêu
        public double? MemberPointsEarnPLH { get; set; }       
        public double? MemberPointsRedeemPLH { get; set; }    
        public string CardCrownXCVL { get; set; }          // Thẻ CrownX
        public string CardCrownXPLH { get; set; }          // Thẻ Phuc Long
    }

    // ---- 16/10/2023 ----

    public class ViewDetailPromotionVoucherCouponModel
    {
        public string OrderNo { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminalNo { get; set; }
        public string OrderDateStr { get; set; }
        public int OrderLineNo { get; set; }
        public int LineNo { get; set; }
        public int LineType { get; set; }
        public string OfferType { get; set; }
        public string OfferNo { get; set; }
        public string ItemNo { get; set; }
        public string Barcode { get; set; }
        public string Description { get; set; }
        public string UOM { get; set; }        
        public Nullable<double> Quantity { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public Nullable<double> DiscountAmount { get; set; }
        public int Total { get; set; }
    }

    public class ExportViewDetailPromotionVoucherCouponModel
    {
        public string OrderNo { get; set; }
        public string OrderDateStr { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminalNo { get; set; }
        public int OrderLineNo { get; set; }
        public int LineNo { get; set; }
        public int LineType { get; set; }
        public string OfferType { get; set; }
        public string OfferNo { get; set; }
        public string ItemNo { get; set; }
        public string Barcode { get; set; }
        public string Description { get; set; }
        public string UOM { get; set; }
        public Nullable<double> Quantity { get; set; }
        public Nullable<double> UnitPrice { get; set; }
        public Nullable<double> DiscountAmount { get; set; }
    }

    public class ExportDetailOrderListModel
    {
        public string OrderNo { get; set; }
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public string Barcode { get; set; }
        public double Quantity { get; set; }            // So luong
        public double UnitPrice { get; set; }           // Gia ban
        public double DiscountAmount { get; set; }
        public double LineAmountIncVAT { get; set; }    // Thanh tien
        public double VATAmount { get; set; }           // Thue
        public double MemberPointsRedeem { get; set; }
        public double MemberPointsEarn { get; set; }
        public double AmountCalPoint { get; set; }
        public string BlockedMemberPoint { get; set; }

    }

    public class PaymentDetailOrderResponseModel
    {
        public int ID { get; set; }
        public string OrderNo { get; set; }                         // Số đơn hàng
        public string TenderType { get; set; }                      // Mã hình thức thanh toán
        public string TenderTypeName { get; set; }                  // Tên hình thức thanh toán
        public string AmountTenderedStr { get; set; }               // Tổng số tiền
        public double AmountTendered { get; set; }                  // Tổng số tiền
        public string ReferenceNo { get; set; }                     // Số tham chiếu
        public string ApprovalCode { get; set; }                    // Mã chuẩn chi
        public string BankPOSCode { get; set; }                     // Máy thanh toán ngân hàng
        public string BankCardType { get; set; }                    // Loại thẻ thanh toán
        public string IsOnline { get; set; }

    }

    public class UpdateSalesTypeForOrderModel
    {       
        public string OrderNo { get; set; }                 // Số đơn hàng
        public string StoreNo { get; set; }                 // Mã cửa hàng
        public string SalesType { get; set; }               // Mã hình thức bán hàng
       
    }




}
