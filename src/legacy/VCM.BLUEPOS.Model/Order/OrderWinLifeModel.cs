using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Order.OrderWinLifeModel
{
    public class OrderListWinLifeModel
    {
        public int ID { get; set; }
        public string OrderNo { get; set; }
        public string StartingTimeStr { get; set; }
        public string OrderTimeStr { get; set; }
        public string OrderDateStr { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminalNo { get; set; }
        public string CashierID { get; set; }
        public double? TotalAmount { get; set; }        //  AmountInclVAT
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

    public class OrderListWinLifeResponseModel
    {
        public int ID { get; set; }
        public string OrderNo { get; set; }
        public string EndingTimeStr { get; set; }
        public string TimeStr { get; set; }
        public string OrderDate { get; set; }           
        public string StoreNoPLG { get; set; }               
        public string StoreNoWCM { get; set; }                 
        public string POSTerminalNo { get; set; }           
        public Nullable<double> BeforeTotalAmount { get; set; }     // Tổng số tiền
        public Nullable<double> DiscountAmount { get; set; }        // Giảm giá tổng bill
        public Nullable<double> TotalAmount { get; set; }           // Thành tiền
        public string RefKey { get; set; }                          // Đơn hàng gốc
        public string OrderType { get; set; }                       // SalesIsReturn : 0 : DH bán, 1 : DH trả 
        public string TransactionType { get; set; }                 // SalesIsReturn : 0 : DH bán, 1 : DH trả 
        public string ChanelSales { get; set; }                     // Kenh ban hang
        public int Total { get; set; }

    }

    public class ExportOrderListWinLifeResponseModel
    {
        public string OrderNo { get; set; }
        public string EndingTimeStr { get; set; }
        public string TimeStr { get; set; }
        public string OrderDate { get; set; }
        public string StoreNoPLG { get; set; }
        public string StoreNoWCM { get; set; }
        public string POSTerminalNo { get; set; }              
        public Nullable<double> BeforeTotalAmount { get; set; }    // Tổng số tiền
        public Nullable<double> DiscountAmount { get; set; }       // Giảm giá tổng bill
        public Nullable<double> TotalAmount { get; set; }          // Thành tiền
        public string OrderType { get; set; }                       // SalesIsReturn : 0 : DH bán, 1 : DH trả 
        public string TransactionType { get; set; }                 // SalesIsReturn : 0 : DH bán, 1 : DH trả 
        public string RefKey { get; set; }                          // Đơn hàng gốc
        public string ChanelSales { get; set; }                     // Kenh ban hang

    }

    public class DetailOrderListWinLifeResponseModel
    {
        public string OrderNo { get; set; }
        public int LineNo { get; set; }
        public string ItemNo { get; set; }
        public string ItemNoSAP { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public double Quantity { get; set; }              
        public double UnitPrice { get; set; }           
        public double DiscountAmount { get; set; }
        public double LineAmountIncVAT { get; set; }       // Thành tiền
        public double VATAmount { get; set; }              // Thuế
    }

    public class ExportDetailOrderListWinLifeModel
    {
        public string OrderNo { get; set; }
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public string Barcode { get; set; }
        public double Quantity { get; set; } // So luong
        public double UnitPrice { get; set; }  // Gia ban
        public double DiscountAmount { get; set; }
        public double LineAmountIncVAT { get; set; }  // Thanh tien
        public double VATAmount { get; set; } // Thue
        public double MemberPointsRedeem { get; set; }
        public double MemberPointsEarn { get; set; }
        public double AmountCalPoint { get; set; }
        public string BlockedMemberPoint { get; set; }

    }

    public class PaymentDetailOrderWinLifeResponseModel
    {
        public int ID { get; set; }
        public string OrderNo { get; set; }                         // Số đơn hàng
        public string TenderType { get; set; }                      // Mã hình thức thanh toán
        public double AmountTendered { get; set; }                  // Tổng số tiền
        public string ReferenceNo { get; set; }                     // Số tham chiếu
        //public string TenderTypeName { get; set; }                  // Tên hình thức thanh toán
        //public string AmountTenderedStr { get; set; }               // Tổng số tiền
        //public string ApprovalCode { get; set; }                    // Mã chuẩn chi
        //public string BankPOSCode { get; set; }                     // Máy thanh toán ngân hàng
        //public string BankCardType { get; set; }                    // Loại thẻ thanh toán
        //public string IsOnline { get; set; }

    }




}
