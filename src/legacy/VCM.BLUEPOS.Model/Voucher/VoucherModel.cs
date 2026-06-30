using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VCM.BLUEPOS.Model.Enums;

namespace VCM.BLUEPOS.Model.Voucher
{
    public class CreateVoucherCouponResponseModel
    {
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public string DiscountType { get; set; }
        public string DiscountValue { get; set; }
        public string MaxAmount { get; set; }
        public string ArticleType { get; set; }
        public string ValueOfVoucher { get; set; }
        public DateTime StartingDate { get; set; }
        public DateTime EndingDate { get; set; }
        public string StartingDateStr { get; set; }
        public string EndingDateStr { get; set; }
        public string BlockedStr { get; set; }
        public string CouponCode { get; set; }
        public string LimitQty { get; set; }
        public DateTime LastDateModified { get; set; }
        public long? Counter { get; set; }
        public bool? IsCheckItem { get; set; }
        public string Pkey { get; set; }
        public string CreatedUser { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<CreateVoucherLineModel> ListItemApply { get; set; }
        public string ListItemApplyStr { get; set; }

    }
    public class CreateVoucherCouponModel
    {
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public int DiscountType { get; set; }
        public float DiscountValue { get; set; }
        public float MaxAmount { get; set; }
        public string ArticleType { get; set; }
        public float ValueOfVoucher { get; set; }
        public DateTime StartingDate { get; set; }
        public DateTime EndingDate { get; set; }
        public string StartingDateStr { get; set; }
        public string EndingDateStr { get; set; }
        public Byte Blocked { get; set; }
        public string CouponCode { get; set; }
        public int LimitQty { get; set; }
        public DateTime LastDateModified { get; set; }
        public long? Counter { get; set; }
        public bool? IsCheckItem { get; set; }
        public string Pkey { get; set; }
        public string CreatedUser { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<CreateVoucherLineModel> ListItemApply { get; set; }
        public string ListItemApplyStr { get; set; }

    }

    public class VoucherCouponHeaderListModel
    {
        public int ID { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public int DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal MaxAmount { get; set; }
        public string ArticleType { get; set; }
        public decimal ValueOfVoucher { get; set; }       
        public Byte? Blocked { get; set; }
        public string CouponCode { get; set; }
        public int LimitQty { get; set; }
        public bool? IsCheckItem { get; set; }
        public string StartingDateStr { get; set; }
        public string EndingDateStr { get; set; }
        public string LastDateModifiedStr { get; set; }        
        public string CreatedUser { get; set; }
        public string CreatedDate { get; set; }
        public int Total { get; set; }

    }

    public class UpdateVoucherHeaderResponseModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public string DiscountType { get; set; }
        public string DiscountValue { get; set; }
        public string MaxAmount { get; set; }
        public string ArticleType { get; set; }
        public string ValueOfVoucher { get; set; }
        public string BlockedStr { get; set; }
        public string CouponCode { get; set; }
        public string LimitQty { get; set; }
        public bool? IsCheckItem { get; set; }
        public string StartingDateStr { get; set; }
        public string EndingDateStr { get; set; }
        public string UpdatedUser { get; set; }
        public DateTime UpdatedDate { get; set; }

    }

    public class UpdateVoucherHeaderModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string UnitOfMeasure { get; set; }
        public int DiscountType { get; set; }
        public Nullable<double> DiscountValue { get; set; }
        public Nullable<double> MaxAmount { get; set; }
        public string ArticleType { get; set; }
        public Nullable<double> ValueOfVoucher { get; set; }
        public byte Blocked { get; set; }
        public string CouponCode { get; set; }
        public int LimitQty { get; set; }
        public bool? IsCheckItem { get; set; }
        public DateTime StartingDate { get; set; }
        public DateTime EndingDate { get; set; }
        public DateTime LastDateModified { get; set; }
        public string UpdatedUser { get; set; }
        public DateTime UpdatedDate { get; set; }

    }

    public class DeleteVoucherHeaderModel
    {
        public string ItemNo { get; set; }
    }

    public class ExportVoucherHeaderListModel
    {
        public string CouponCode { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string ArticleType { get; set; }
        public string UnitOfMeasure { get; set; }
        public Byte? Blocked { get; set; }
        public decimal ValueOfVoucher { get; set; }
        public int DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal MaxAmount { get; set; }   
        public int LimitQty { get; set; }
        public bool? IsCheckItem { get; set; }
        public string StartingDateStr { get; set; }
        public string EndingDateStr { get; set; }
        public string LastDateModifiedStr { get; set; }
 
    }

    public class CreateVoucherLineModel
    {
        public string ItemNo { get; set; }
        public int LineNo { get; set; }
        public string LineItemNo { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public string Barcode { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string CreatedUser { get; set; }
        public DateTime CreatedDate { get; set; }

    }

    public class VoucherLineResponseModel
    {
        public int ID { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string ArticleType { get; set; }
        public string CouponCode { get; set; }
        public int LineNo { get; set; }
        public string LineItemNo { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public string Barcode { get; set; }
        public Byte? Blocked { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public int Total { get; set; }

    }

    public class DeleteVoucherLineModel
    {
        public string Pkey { get; set; }
    }

    public class UpdateVoucherLineResponseModel
    {
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string CouponCode { get; set; }
        public int LineNo { get; set; }
        public string LineItemNo { get; set; }
        public string ArticleType { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public string Barcode { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string UpdatedUser { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class UpdateVoucherLineModel
    {
        public string ItemNo { get; set; }
        public int LineNo { get; set; }
        public string LineItemNo { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }
        public string Barcode { get; set; }
        public long? Counter { get; set; }
        public string Pkey { get; set; }
        public string UpdatedUser { get; set; }
        public DateTime UpdatedDate { get; set; }

    }

    public class ExportVoucherLineResponseModel
    {
        public string CouponCode { get; set; }
        public string ItemNo { get; set; }
        public string ItemName { get; set; }
        public string ArticleType { get; set; }
        public string Blocked { get; set; }
        public string Barcode { get; set; }
        public string LineItemNo { get; set; }
        public string Description { get; set; }
        public string UnitOfMeasure { get; set; }

    }

    public class VoucherPublishedResponseModel
    {
        public string BonusBuy { get; set; }
        public string ArticleNo { get; set; }
        public string OrderNo { get; set; }
        public string SerialNo { get; set; }
        public string VoucherType { get; set; }
        public string VoucherValue { get; set; }
        public string Status { get; set; }
        public string StoreNo { get; set; }
        public string PosNo { get; set; }
        public string ItemName { get; set; }
        public string TranDateStr { get; set; }    
        public string MaxQtyUse { get; set; }
        public string MaxAmount { get; set; }
        public string MaxQuantityIssue { get; set; }
        public string ApplyType { get; set; }
        public string IsVinID { get; set; }
        public string IsOffline { get; set; }
        public string IsSend { get; set; }
        public string IsCheckItem { get; set; }
        public string FromDateStr { get; set; }
        public string EnDateStr { get; set; }
        public string CreatedDateStr { get; set; }
        public int Total { get; set; }
    }

    public class CheckVoucherResponse
    {
        public string Status { get; set; }
        public string Return { get; set; }
        public string ActicleNo { get; set; }
        public string ActicleType { get; set; }
        public string VoucherNumber { get; set; }
        public decimal? Value { get; set; }
        public string Voucher_Currency { get; set; }
        public string Validity_From_Date { get; set; }
        public string Expiry_Date { get; set; }
        public string FormatValue { get { return string.Format("{0:###,###}", Value); } }

    }

    public class ListVoucherResendModel
    {
        public string SerialNo { get; set; }
        public string Article_No { get; set; }
        public double? Value { get; set; }
        public DateTime? ValidityFromDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string From_Date { get { return string.Format("{0:dd/MM/yyyy}", ValidityFromDate); } }
        public string Expiry_Date { get { return string.Format("{0:dd/MM/yyyy}", ExpiryDate); } }
        public string Status { get; set; }
        public string BonusBuy { get; set; }
        public string SiteCode { get; set; }
        public string ArticleType { get; set; }
        public string VoucherNumber { get; set; }
        public string ArticleNo { get; set; }
        public bool? Result { get; set; }
        public string MessageResult { get; set; }
    }

    public class VoucherStatusResponse
    {
        public string VoucherNumber { get; set; }
        public string Status { get; set; }
        public string Return { get; set; }
    }

    public class ListVoucherModel
    {
        public string OrderNo { get; set; }
        public string SerialNo { get; set; }
    }

    public class ExportExcelVoucherPublishedResponseModel
    {
        public string StoreNo { get; set; }
        public string PosNo { get; set; }
        public string BonusBuy { get; set; }
        public string SerialNo { get; set; }
        public string OrderNo { get; set; }
        public string ArticleNo { get; set; }
        public string ItemName { get; set; }
        public string ApplyType { get; set; }
        public string Status { get; set; }
        public string VoucherValue { get; set; }
        public string MaxQtyUse { get; set; }
        public string MaxAmount { get; set; }
        public string MaxQuantityIssue { get; set; }
        public string TranDateStr { get; set; }      
        public string IsOffline { get; set; }
        public string IsCheckItem { get; set; }
        public string VoucherType { get; set; }
        public string CreatedDateStr { get; set; }

    }


    // 02/07/2025,tungnt : resend voucher SAP

    public class VoucherResendSAPModel
    {
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string UserPOS { get; set; }
        public List<ListSerialResendSAPModel> ListSerial { get; set; }
    }
    public class ListSerialResendSAPModel
    {
        public string Type { get; set; }
        public string ArticleNo { get; set; }
        public string SerialNumber { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
    }
    public class ListSerialResendSAP
    {
        public string SerialNumber { get; set; }
    }

    public class SerialResultResendSAPModel
    {
        public HttpStatusCode Status { get; set; }
        public string Message { get; set; }
        public List<ListSerialResendSAP> Data { get; set; }
        public string Item1 { get; set; }
        public string Item2 { get; set; }
        public string Item3 { get; set; }
        
    }

}
