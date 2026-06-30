using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class PrintAgainModel
    {
        public string BranchInvoice { get; set; }
        public string DocumentNo { get; set; }
        public string BranchNo { get; set; }
        public string KeyPdf { get; set; }
        public string Key { get; set; }
        public string FKey { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string StoreNo { get; set; }
        public string InvoicePattern { get; set; }
        public string SerialNo { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime? ArisingDate { get; set; }
        public DateTime? ArisingToDate { get { return ArisingDate; } }

        public DateTime? CreateDate { get; set; }
        public DateTime? BusinessDate { get; set; }
        public string CusCode { get; set; }
        public string CusName { get; set; }
        public string CusCom { get; set; }
        public string CusPhone { get; set; }
        public string CusTaxCode { get; set; }
        public string CusAddress { get; set; }
        public string EmailDeliver { get; set; }
        public string InvoiceType { get; set; }
        public string InvoiceTypeName { get; set; }
        public string VinTraLai { get; set; }
        public string Remark { get; set; }
        public bool? IsSigned { get; set; }
        public bool? IsAllow { get; set; }
        public bool? IsEOD { get; set; }
        public bool? IsCancel { get; set; }
        public bool? IsDummy { get; set; }
        public string TinhTrangKySo { get; set; }
        public string HinhThuc { get; set; }
        public int? TotalRow { get; set; }
        public string NgayPhatHanh { get { return string.Format("{0:dd/MM/yyyy}", ArisingDate); } }
        public string NgayTao { get { return string.Format("{0:dd/MM/yyyy HH:mm:ss}", CreateDate); } }
        public string NgayKD { get { return string.Format("{0:dd/MM/yyyy}", BusinessDate); } }
    }
}
