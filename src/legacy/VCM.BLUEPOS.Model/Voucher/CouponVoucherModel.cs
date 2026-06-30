using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Voucher
{
    public class CouponVoucherModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public string DiscountValue { get; set; }
        public string DiscountType { get; set; }
        public string MaxAmount { get; set; }
        public string CouponCode { get; set; }
        public string ArticleType { get; set; }
        public string SaleType { get; set; }
        public string ValueOfVoucher { get; set; }
        public string StartingDateStr { get; set; }
        public string EndingDateStr { get; set; }
        public string LastDateModified { get; set; }
        public string Blocked { get; set; }
        public string CpnVchType {get; set;}
        public string TotalBill { get; set; }
        public int Total { get; set; }
    }

    public class ExportExcel_VoucherCouponModel
    {
        public string ItemNo { get; set; }              // Ma Voucher/Coupon
        public string ItemName { get; set; }            // Ten Voucher/Coupon
        public string CouponCode { get; set; }          // Serial Number
        public string ArticleType { get; set; }         // Loại Voucher/Coupon
        public string DiscountType { get; set; }        // Loai giam gia
        public string Blocked { get; set; }             // Trang thai
        public string CpnVchType { get; set; }          // Loai the ap dung
        public string SaleType { get; set; }            // Hình thuc ban hang        
        public string TotalBill { get; set; }           // Ap dung total bill
        public string UnitOfMeasure { get; set; }       // ĐVT
        public string DiscountValue { get; set; }       // Giá trị giảm    
        public string ValueOfVoucher { get; set; }      // Giá trị voucher/coupon
        public string StartingDateStr { get; set; }     // Ngay bat dau co hieu luc
        public string EndingDateStr { get; set; }       // Ngay bat dau het hieu luc
        public string LastDateModified { get; set; }    // Ngay cap nhat
             
    }

    public class DetailCouponVoucherHeaderResponseModel
    {
        public string ItemNo { get; set; }              // Ma Voucher/Copuon
        public string ItemName { get; set; }            // Ten Voucher/Coupon
        public string CouponCode { get; set; }          // Serial Number
        public string UnitOfMeasure { get; set; }
        public string DiscountType { get; set; }
        public string CpnVchType { get; set; }
        public string SaleType { get; set; }
        public string DiscountValue { get; set; }
        public string MinAmount { get; set; }
        public string MaxAmount { get; set; }
        public string ArticleType { get; set; }
        public string ValueOfVoucher { get; set; }     
        public string Blocked { get; set; }      
        public int LimitQty { get; set; }
        public int ApplyDiscountType { get; set; }  // 29/05/2025,tungnt8
        public int IsCheckItem { get; set; }  // 29/05/2025,tungnt8        
        public string StartingDateStr { get; set; }
        public string EndingDateStr { get; set; }
        public string LastDateModified { get; set; }
        public string TotalBill { get; set; }

    }

    public class DetailCouponVoucherLineResponseModel
    {
        public string ItemNo { get; set; }
        public int LineNo { get; set; }
        public string ApplyItemNoSAP { get; set; } 
        public string ApplyItemNoPLG { get; set; }   
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public string Barcode { get; set; }
        public string TaxCode { get; set; }
        public string Size { get; set; }
        public int Total { get; set; }
    }

    public class ExportExcel_DetailCouponVoucherLineModel
    {
        public string ItemNo { get; set; }          // Ma voucher/coupon
        public int LineNo { get; set; }
        public string Barcode { get; set; }
        public string ApplyItemNoSAP { get; set; }  // Ma ItemNo SAP
        public string ApplyItemNoPLG { get; set; }  // Ma ItemNo PLG
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public string Size { get; set; }
        public string TaxCode { get; set; }
    }











}
