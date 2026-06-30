using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class InvoiceResultModel
    {
        public string BranchID { get; set; }
        public string InvoiceNo { get; set; }
        public string SerialNo { get; set; }
        public string InvoicePattern { get; set; }
        public string Key { get; set; }
        public string ERR { get; set; }
        public string ERRMSG { get; set; }
        public string Salt { get; set; }
        public string SaltEncrypt { get; set; }
        public string BranchName { get; set; }

    }
}
