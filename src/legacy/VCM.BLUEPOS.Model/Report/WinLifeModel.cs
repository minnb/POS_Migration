using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report.WinLifeModel
{
    public class DetailRevenueReportWinLifeResponseModel
    {
        public int ID { get; set; }
        public string OrderNo { get; set; }                                 // Số đơn hàng
        public string EndingTimeStr { get; set; }
        public string TimeStr { get; set; }
        public string OrderDate { get; set; }
        public string StoreNoPLG { get; set; }
        public string StoreNoWCM { get; set; }
        public string POSTerminalNo { get; set; }
        public string OrigOrder { get; set; }                               // Đơn hàng gốc
        public string OrderType { get; set; }                               // SalesIsReturn : 0 : DH ban, 1 : DH tra  
        public string TransactionType { get; set; }
        public string ItemNo { get; set; }                                 // Mã sản phẩm
        public string ItemNoSAP { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }                 
        public Nullable<double> Quantity { get; set; }                     // Số lượng
        public Nullable<double> UnitPrice { get; set; }                    // Đơn giá
        public Nullable<double> DiscountAmount { get; set; }                // Giảm giá
        public Nullable<double> LineAmountBeforeVAT { get; set; }           // Thành tiền trước thuế 
        public string VATCode { get; set; }                                 // Tỷ lệ thuế suất % VAT
        public Nullable<double> VATAmount { get; set; }                     // Tiền thuế VAT
        public Nullable<double> LineAmountIncVAT { get; set; }              // Thành tiền sau thuế VAT
        public int Total { get; set; }
        public string ChanelSales { get; set; }     // Kenh ban hang
        public string Size { get; set; }
        public string CupName { get; set; }         // ten loai ly

    }

    public class ExportExcelDetailWinLifeRevenueReportModel
    {
        public string EndingTimeStr { get; set; }
        public string TimeStr { get; set; }
        public string OrderDate { get; set; }
        public string StoreNoPLG { get; set; }
        public string StoreNoWCM { get; set; }
        public string POSTerminalNo { get; set; }
        public string OrderNo { get; set; }                        // Số đơn hàng       
        public string OrderType { get; set; }                      // Hình thuc ban hang 
        public string OrigOrder { get; set; }                      // Đơn hàng gốc
        public string TransactionType { get; set; }                 // Loại đơn hang
        public string ItemNo { get; set; }                          // Mã sản phẩm PLG
        public string ItemNoSAP { get; set; }                       // Mã sản phẩm SAP
        public string Description { get; set; }                     // Tên sản phẩm 
        public string UnitOfMeasure { get; set; }                   // Đơn vị tính       
        public Nullable<double> Quantity { get; set; }              // Số lượng
        public Nullable<double> UnitPrice { get; set; }             // Đơn giá
        public Nullable<double> DiscountAmount { get; set; }        // Giảm giá
        public Nullable<double> LineAmountBeforeVAT { get; set; }   // Thành tiền trước thuế VAT
        public string VATCode { get; set; }                         // Tỷ lệ thuế suất %
        public Nullable<double> VATAmount { get; set; }             // Tiền thuế VAT
        public Nullable<double> LineAmountIncVAT { get; set; }      // Thành tiền sau thuế VAT
        public string ChanelSales { get; set; }                     // Kenh ban hang
        public string Size { get; set; }
        public string CupName { get; set; }                         // ten loai ly

    }


    public class PaymentOrderSalesWinLifeResponseModel
    {
        public int ID { get; set; }
        public string OrderNo { get; set; }                                 // Số đơn hàng
        public string PaymentDay { get; set; }                              // Ngày thanh toán 
        public string StoreNoPLG { get; set; }                              // Cửa hàng
        public string StoreNoWCM { get; set; }
        public string PosNo { get; set; }                                   // Máy POS
        public string TransactionType { get; set; }     // Loại đơn hàng             
        public string OrderType { get; set; }           // Hinh thuc ban hang                            
        public string TenderType { get; set; }          // Hinh thuc thanh toan
        public Nullable<double> AmountTendered { get; set; }                          // Tổng số tiền
        public string CurrencyUnit { get; set; }                            // Đơn vị tiền tệ
        public string ReferenceNo { get; set; }                             // Số tham chiếu
        public string ChanelSales { get; set; }                             // Kenh ban hang
        public int Total { get; set; }

    }

    public class ExportExcelPaymentOrderSalesWinLifeModel
    {
        public string OrderNo { get; set; }                                 // Số đơn hàng
        public string PaymentDay { get; set; }                              // Ngày thanh toán
        public string StoreNoPLG { get; set; }                              // Cửa hàng Phuc Long
        public string StoreNoWCM { get; set; }
        public string PosNo { get; set; }                                   // Máy POS
        public string OrderType { get; set; }               // Hinh thuc ban hang
        public string TransactionType { get; set; }         // Loai don hang
        public string TenderType { get; set; }              // Hinh thuc thanh toan
        public Nullable<double> AmountTendered { get; set; }                // Tổng số tiền
        public string CurrencyUnit { get; set; }                            // Đơn vị tiền tệ
        public string ReferenceNo { get; set; }                             // Số tham chiếu
        public string ChanelSales { get; set; }                             // Kenh ban hang
    }




}
