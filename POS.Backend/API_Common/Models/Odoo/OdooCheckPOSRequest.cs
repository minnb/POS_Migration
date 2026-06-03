using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Odoo
{
    public class OdooCheckPOSRequest
    {
        public string partner { get; set; }
        public string storeNo { get; set; }
        public string posID { get; set; }
        public string isVoucher { get; set; }
        public string seriNo { get; set; }
        public List<OdooCheckVoucherRequest> listVoucher { get; set; }
    }
    public class OdooCheckVoucherRequest
    {
        public string seriNo { get; set; }
        public string articleSAP { get; set; }
        public string isEmployee { get; set; }
    }
}
