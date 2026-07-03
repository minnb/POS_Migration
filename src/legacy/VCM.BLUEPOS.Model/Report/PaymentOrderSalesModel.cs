using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class PaymentOrderSalesResponseModel
    {
        public int ID { get; set; }
        public string OrderNo { get; set; }                                 // Số đơn hàng
        public string BusinessDay { get; set; }                             // Ngày kinh doanh
        public string PaymentDay { get; set; }                              // Ngày thanh toán
        public string StoreNo { get; set; }                                 // Cửa hàng
        public string PosNo { get; set; }                                   // Máy POS
        public string SalesType { get; set; }                               // Hình thức bán hàng
        public string ReceiptNo { get; set; }                               // Số ReceiptNo
        public string TenderType { get; set; }                              // Mã hình thức thanh toán
        public string TenderTypeName { get; set; }                          // Tên hình thức thanh toán
        public string OrderType { get; set; }                               // Loại đơn hàng
        public double AmountTendered { get; set; }                          // Tổng số tiền
        public string CurrencyUnit { get; set; }                            // Đơn vị tiền tệ
        public string ReferenceNo { get; set; }                             // Số tham chiếu
        public string ApprovalCode { get; set; }                            // Mã chuẩn chi
        public string BankPOSCode { get; set; }                             // Máy thanh toán ngân hàng
        public string BankCardType { get; set; }                            // Loại thẻ thanh toán
        public string IsOnline { get; set; }                                // Trạng thái Online
        public string StaffID { get; set; }                                 // Thu ngân
        public int Total { get; set; }
        public string PartnerCode { get; set; }
        public string UserID { get; set; }                                  // Nơi tạo bill
        public string RefKey1 { get; set; }                                 // So DH tham chieu        
        public string CurrencyCode { get; set; }                            // Đơn vị ngoại tệ
        public Nullable<double> AmountInCurrency { get; set; }              // Tiền nguyên tệ
        public Nullable<double> ExchangeRate { get; set; }                  // Tỷ giá

        //public double CommissionValue { get; set; }                       // Chiet khau hoa hong

    }

    public class ExportExcelPaymentOrderSalesModel
    {
        public string OrderNo { get; set; }                                 // Số đơn hàng
        public string BusinessDay { get; set; }                             // Ngày kinh doanh
        public string PaymentDay { get; set; }                              // Ngày thanh toán
        public string StoreNo { get; set; }                                 // Cửa hàng
        public string PosNo { get; set; }                                   // Máy POS
        public string SalesType { get; set; }                               // Hình thức bán hàng
        public string OrderType { get; set; }                               // Loại đơn hàng
        public string TenderType { get; set; }                              // Mã hình thức thanh toán
        public string TenderTypeName { get; set; }                          // Tên hình thức thanh toán        
        public double AmountTendered { get; set; }                          // Tổng số tiền
        public string CurrencyUnit { get; set; }                            // Đơn vị tiền tệ
        public string ReferenceNo { get; set; }                             // Số tham chiếu
        public string ApprovalCode { get; set; }                            // Mã chuẩn chi
        public string BankPOSCode { get; set; }                             // Máy thanh toán ngân hàng
        public string BankCardType { get; set; }                            // Loại thẻ thanh toán
        public string ReceiptNo { get; set; }                               // Số ReceiptNo
        public string IsOnline { get; set; }                                // Trạng thái Online
        public string PartnerCode { get; set; }                             // Đối tác
        public string StaffID { get; set; }                                 // Thu ngân
        public string UserID { get; set; }                                  // Nơi tạo bill
        public string RefKey1 { get; set; }                                 // So DH tham chieu    
        public string CurrencyCode { get; set; }                            // Đơn vị ngoại tệ
        public Nullable<double> AmountInCurrency { get; set; }              // Tiền nguyên tệ
        public Nullable<double> ExchangeRate { get; set; }                  // Tỷ giá

        //public double CommissionValue { get; set; }                       // Chiet khau hoa hong

    }


    public class GeneralPaymentOrderSalesModel
    {
        public int ID { get; set; }
        public DateTime PaymentDate { get; set; }       // Ngày thanh toán
        public string StoreNo { get; set; }             
        public string StoreName { get; set; }           
        public string TenderType { get; set; }          // Mã hình thức thanh toán
        public string TenderTypeName { get; set; }      // Tên hình thức thanh toán       
        public double AmountTendered { get; set; }      // Doanh thu       
        public int CountOrder { get; set; }             // Số luong bill
        public int Total { get; set; }
        
    }

    public class ExportExcel_GeneralPaymentOrderSalesModel
    {
        public int ID { get; set; }
        public string PaymentDateStr { get; set; }          // Ngày thanh toán
        public string StoreNo { get; set; }                 
        public string StoreName { get; set; }
        public string TenderType { get; set; }              // Mã hình thức thanh toán
        public string TenderTypeName { get; set; }          // Tên hình thức thanh toán       
        public double AmountTendered { get; set; }          // Doanh thu       
        public int CountOrder { get; set; }                 // Số luong bill
    }




}
