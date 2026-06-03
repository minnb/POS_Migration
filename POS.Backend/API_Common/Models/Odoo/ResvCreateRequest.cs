using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Odoo
{
    public class ResvCreatePOSRequest
    {
        public string storeNo { get; set; }
        public string posID { get; set; }

        public DT_RESV_CR8_ReqPOSHeader reqHeader { get; set; }
        public List<DT_RESV_CR8_ReqPOSItem> reqItem { get; set; }
    }

    public class DT_RESV_CR8_ReqPOSHeader
    {
        public string Plant { get; set; }
        public string ResDate { get; set; }
        public string MoveType { get; set; }
        public string CostCenter { get; set; }
        public string RefNo { get; set; }
        public string Order { get; set; }
    }
    public class DT_RESV_CR8_ReqPOSItem
    {
        public string StorageLocation { get; set; }
        public string Material { get; set; }
        public string Quantity { get; set; }
        public string Unit { get; set; }
        public string ShortText { get; set; }
        public string Movement { get; set; }
    }
}
