using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.VinID
{
    public class HeaderTransactionBluePointsModel
    {
        public int? ID { get; set; }
        public string StoreNo { get; set; }
        public string POSCode { get; set; }
        public string TransactionType { get; set; }
        public string CustomerName { get; set; }
        public double TotalBillAmount { get; set; }
        public string OrderNo { get; set; }
        public string ReceiptNumber { get; set; }
        public string VinidCardNumber { get; set; }
        public string VinIDCSN { get; set; }            // Ma nhan vien VinID CSN
        public string CashierCode { get; set; }
        public string ReferenceNumber { get; set; }
        public double ExtraEarnByItems { get; set; }
        public double ExtraEarnByCampaign { get; set; }
        public string OriginalReceiptNumber { get; set; }
        public string OriginalReferenceNumber { get; set; }
        public string CreatedDate { get; set; }
        public int Total { get; set; }
    }
    public class DetailTransactionBluePointsModel
    {
        public int? ID { get; set; }
        public string Barcode { get; set; }
        public double RecordNo { get; set; }
        public string Article { get; set; }                     // Mã sản phẩm
        public string ArticleName { get; set; }                 // Tên sản phẩm
        public string UOM { get; set; }                         // Đơn vị tính
        public double Quantity { get; set; }                     // Số lượng
        public double SalePrice { get; set; }                    // Đơn giá
        public double Amount { get; set; }                       // Thành tiền
        public double DiscountAmount { get; set; }               // Khuyến mãi
        public double LineAmount { get; set; }                   // Thành tiền sau khuyến mãi
        public double ExtraQuantityEarn { get; set; }            // SL tích điểm
        public double ExtraAmountEarn { get; set; }              // Điểm
        public int Total { get; set; }
    }

    public class ExportTransactionBluePointsModel
    {
        public string ReceiptNumber { get; set; }               // Số receipt
        public string OrderNo { get; set; }                     // Số OrderNo
        public string CreatedDate { get; set; }                 // Thời gian
        public string StoreNo { get; set; }                     // Mã cửa hàng
        public string POSCode { get; set; }                     // Mã máy POS
        public string TransactionType { get; set; }             // Loại giao dịch
        public string VinidCardNumber { get; set; }             // Số thẻ VinID
        public string CustomerName { get; set; }                // Tên khách hàng
        public double TotalBillAmount { get; set; }             // Tổng số tiền
        public double ExtraEarnByItems { get; set; }            // Điểm KM Masan
        public double ExtraEarnByCampaign { get; set; }         // Điểm nhân viên
        public string Barcode { get; set; }                     // Mã barcode
        public double RecordNo { get; set; }
        public string Article { get; set; }                     // Mã sản phẩm
        public string ArticleName { get; set; }                 // Tên sản phẩm
        public string UOM { get; set; }                         // Đơn vị tính
        public double Quantity { get; set; }                    // Số lượng
        public double SalePrice { get; set; }                   // Đơn giá
        public double Amount { get; set; }                      // Thành tiền
        public double DiscountAmount { get; set; }              // Khuyến mãi
        public double LineAmount { get; set; }                  // Thành tiền sau khuyến mãi
        public double ExtraQuantityEarn { get; set; }           // SL tích điểm
        public double ExtraAmountEarn { get; set; }             // Điểm
        public string CashierCode { get; set; }                 // Thu ngân
        public string VinIDCSN { get; set; }                    // Mã nhân viên CSN
        public string ReferenceNumber { get; set; }             // Số tham chiếu
        public string OriginalReceiptNumber { get; set; }       // Số receipt gốc
        public string OriginalReferenceNumber { get; set; }     // Số tham chiếu gốc
    }



}
