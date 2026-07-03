using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class InvoiceHearderRequest
    {
        public string SetDB { get; set; }
        public string SiteNo { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public string TuNgayStr { get; set; }  // 21/11/2024, tungnt8
        public string DenNgayStr { get; set; } // 21/11/2024, tungnt8
        public string MauSo { get; set; }
        public string SoHD { get; set; }
        public string TenKH { get; set; }
        public string DienThoai { get; set; }
        public string TenCTy { get; set; }
        public string KyHieu { get; set; }
        public string LoaiHoaDon { get; set; }
        public string MST { get; set; }
        public string HinhThuc { get; set; }
        public string IsSigned { get; set; }
        public string IsCancel { get; set; }
        public string StatusCQT { get; set; }
        public string Type { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
        public string Export { get; set; }
    }
    public class InvoiceExportModel
    {
        public string StoreNo { get; set; }
        public string InvoicePattern { get; set; }
        public string SerialNo { get; set; }
        public string InvoiceNo { get; set; }
        public string InvoiceTypeName { get; set; }
        public string CreateDate { get; set; }
        public string BusinessDate { get; set; }
        public string CusName { get; set; }
        public string CusCom { get; set; }
        public string EmailDeliver { get; set; }
        public string CusPhone { get; set; }
        public string CusTaxCode { get; set; }
        public string HinhThuc { get; set; }
        public string Remark { get; set; }

    }

    public class InvoiceCusIssueVATRequest
    {
        public string Type { get; set; }
        public string SetDB { get; set; }
        public string ListStoreJson { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }       
        public string CustomerName { get; set; }
        public string CompanyName { get; set; }
        public string TaxCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string OrderNo { get; set; }
        public string Address { get; set; }     
        public string TextSearch { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
    }

    // 21/11/2024, tungnt8
    public class InvoiceListExportModel
    {
        public string TinhTrangKySo { get; set; }
        public string StoreNo { get; set; }
        public string OrderNo { get; set; }
        public string InvoicePattern { get; set; }
        public string SerialNo { get; set; }
        public string InvoiceNo { get; set; }
        public Nullable<double> Amount { get; set; }
        public string InvoiceTypeName { get; set; }
        public string CreateDate { get; set; }
        public string BusinessDate { get; set; }
        public string CusName { get; set; }
        public string CusCom { get; set; }
        public string CusAddress { get; set; }
        public string EmailDeliver { get; set; }
        public string CusPhone { get; set; }
        public string CusTaxCode { get; set; }
        public string HinhThuc { get; set; }
        public string Remark { get; set; }
        public string BillTaxCode { get; set; }
        public string Key { get; set; }
    }




}
