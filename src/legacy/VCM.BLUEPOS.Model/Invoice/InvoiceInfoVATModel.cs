using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class InvoiceInfoVATModel
    {
        public string InvoicePattern { get; set; }//Mẫu số
        public string InvoiceNo { get; set; }//Số HĐ GTGT
        public string SerialNo { get; set; }//Ký hiệu
        public string SaltEncrypt { get; set; }//Mã bí mật
    }
}
