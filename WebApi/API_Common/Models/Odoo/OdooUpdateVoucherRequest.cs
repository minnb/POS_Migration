using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Odoo
{
    public class OdooUpdateVoucherRequest
    {
        public string seriNo { get; set; }
        public string isVoucher { get; set; }
        public string statusCode { get; set; }
        public string salePlant { get; set; }
        public string salePrice { get; set; }
        public string discount { get; set; }
        public string saleDate { get; set; }
        public string saleTrans { get; set; }
        public string redeemAmount { get; set; }
        public string redeemPlant { get; set; }
        public string redeemDate { get; set; }
        public string redeemTrans { get; set; }
        public string isActive { get; set; }//25082022
    }
    public class OdooUpdatePOSRequest
    {
        public string partner { get; set; }
        public string storeNo { get; set; }
        public string storeNo_SAP { get; set; }
        public string posID { get; set; }
        public string staffCode { get; set; }
        public string orderNo { get; set; }
        public double totalBill { get; set; }
        public List<OdooUpdateVoucherRequest> listSeriNo { get; set; }
    }

}
