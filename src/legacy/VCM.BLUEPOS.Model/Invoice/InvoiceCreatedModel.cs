using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class InvoiceCreatedModel
    {
        public string SiteNo { get; set; }
        public string IPServer { get; set; }
        public string OrderNo { get; set; }
        public string CustomerName { get; set; }
        public string CompanyName { get; set; }
        public string TaxCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public Nullable<System.Boolean> IsNotVAT { get; set; }
        public string CreatedUser { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public int TotalRow { get; set; }
        public string AddressCus { get; set; }
        public double? PrepaymentAmount { get; set; }
        public int IsEdit { get; set; }
    }
    public class InvoiceCusIssueVATExportModel
    {
        public string SiteNo { get; set; }       
        public string CustomerName { get; set; }
        public string TaxCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string CompanyName { get; set; } 
        public string AddressCus { get; set; }        
        public string CreatedDate { get; set; }
        public string OrderNo { get; set; }
        public double? PrepaymentAmount { get; set; }
    }

    // 26/11/2024,tungnt8
    public class CustomerInfoInvoiceMTTModel
    {
        public string StoreNo { get; set; }
        public string InvoiceNo { get; set; }
        public string BillTaxCode { get; set; }
        public string CompTaxCode { get; set; }
        public string OrderNo { get; set; }
        public string CusName { get; set; }
        public string CusCom { get; set; }
        public string CusTaxCode { get; set; }
        public string CusPhone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string AddressCus { get; set; }
        public Nullable<System.Boolean> IsNotVAT { get; set; }
        public string ArisingDate { get; set; }
        public string CreatedUser { get; set; }
        public double? Amount { get; set; }
        public string InvoicePattern { get; set; } // mau so
        public string SerialNo { get; set; } // ky hieu
        public string InvoiceTypeName { get; set; } // loai hoa don
        public string CreateDate { get; set; } // ngay phat hanh
        public string HinhThuc { get; set; } // hinh thuc
        public string Remark { get; set; }  // thay the / dieu chinh
        public string KeyPdf { get; set; }
        public string Key { get; set; }
        public int Total { get; set; }
    }

    public class ExportCustomerInfoInvoiceMTTModel
    {
        public string StoreNo { get; set; }
        public string CusName { get; set; }
        public string CusTaxCode { get; set; }
        public string CusPhone { get; set; }
        public string Email { get; set; }
        public string CusCom { get; set; }
        public string Address { get; set; }
        public string InvoicePattern { get; set; } // mau so
        public string SerialNo { get; set; } // ky hieu
        public string InvoiceNo { get; set; } // so hoa don
        public string InvoiceTypeName { get; set; } // loai hoa don
        public string ArisingDate { get; set; } // ngay xuat HĐ
        public string CreateDate { get; set; } // ngay phat hanh
        public string HinhThuc { get; set; } // hinh thuc
        public string Remark { get; set; }  // thay the / dieu chinh
        public string OrderNo { get; set; }
        public double? Amount { get; set; }
        public string BillTaxCode { get; set; }
        public string Key { get; set; }
    }

    public class CustomerInfoInvoiceMTTRequest
    {
        public string SetDB { get; set; }
        public string ListStoreJson { get; set; }
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public string TextSearch { get; set; }  // tu khoa
        public string CustomerName { get; set; }
        public string CompanyName { get; set; }
        public string TaxCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string OrderNo { get; set; }
        public string Address { get; set; }
        public int? PageSize { get; set; }
        public int? PageNumber { get; set; }
    }



}
