using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class ExportInvoiceReportModel
    {
        public int TotalRow { get; set; }
        public string SerialNo { get; set; } //[Ký hiệu]
        public string InvoicePattern { get; set; }//[Mẫu số]
        public string VATNumber { get; set; } //[Số HĐ]
        public string IsEOD { get; set; }
        public string CusTaxCode { get; set; }//[MST khách hàng]
        public string CusName { get; set; } //[Người mua hàng]
        public string CusCom { get; set; } //[Tên đơn vị]
        public string EmailDeliver { get; set; }
        public string CusAddress { get; set; }//[Địa chỉ]
        public string SaltEncrypt { get; set; }//[Mã tra cứu]
        public string BusinessDate { get; set; }//[Ngày kinh doanh]
        public string PrintedDate { get; set; }//[Ngày xuát HĐ]
        public string StoreNo { get; set; }//[Mã Site]
        public string DocumentNo { get; set; }// [Số bill]
        public string TotalAmount { get; set; }//[Tổng tiền thanh toán]
        public string MemberPointsEarn { get; set; }//[Tổng điểm tích]
        public string MemberPointsRedeem { get; set; }//[Tổng điểm tiêu]
        public string Barcode { get; set; }
        public string ItemName { get; set; }//[Tên SP]
        public string UnitPrice { get; set; }//[Đơn giá]
        public double Quantity { get; set; }//[Số lượng]
        public string UnitOfMeasure { get; set; }// [ĐVT]
        public string Amount { get; set; }//[Thành tiền]
        public string DiscountAmount { get; set; }//[Giảm giá]
        public string MemberPointsRedeemVINID { get; set; }//[Điểm tiêu VINID]
        public double VATPercent { get; set; }//[Thuế suất]
        public string VATAmount { get; set; }//[Tiền thuế]
    }

    public class ExcelInvoiceReportModel
    {
        public string SerialNo { get; set; } //[Ký hiệu]
        public string InvoicePattern { get; set; }//[Mẫu số]
        public string VATNumber { get; set; } //[Số HĐ]
        public string IsEOD { get; set; }
        public string CusTaxCode { get; set; }//[MST khách hàng]
        public string CusName { get; set; } //[Người mua hàng]
        public string CusCom { get; set; } //[Tên đơn vị]
        public string EmailDeliver { get; set; }
        public string CusAddress { get; set; }//[Địa chỉ]
        public string SaltEncrypt { get; set; }//[Mã tra cứu]
        public string BusinessDate { get; set; }//[Ngày kinh doanh]
        public string PrintedDate { get; set; }//[Ngày xuát HĐ]
        public string StoreNo { get; set; }//[Mã Site]
        public string DocumentNo { get; set; }// [Số bill]
        public string TotalAmount { get; set; }//[Tổng tiền thanh toán]
        public string MemberPointsEarn { get; set; }//[Tổng điểm tích]
        public string MemberPointsRedeem { get; set; }//[Tổng điểm tiêu]
        public string Barcode { get; set; }
        public string ItemName { get; set; }//[Tên SP]
        public string UnitPrice { get; set; }//[Đơn giá]
        public double Quantity { get; set; }//[Số lượng]
        public string UnitOfMeasure { get; set; }// [ĐVT]
        public string Amount { get; set; }//[Thành tiền]
        public string DiscountAmount { get; set; }//[Giảm giá]
        public string MemberPointsRedeemVINID { get; set; }//[Điểm tiêu VINID]
        public double VATPercent { get; set; }//[Thuế suất]
        public string VATAmount { get; set; }//[Tiền thuế]
    }
}
