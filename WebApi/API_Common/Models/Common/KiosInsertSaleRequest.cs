using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Common
{
    public class KiosInsertSalePOSRequest
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; } = "";
        public string SaleJson { get; set; }
    }

    public class KiosInsertSaleRequest
    {
        public string StoreNo { get; set; }
        public string Json { get; set; }
        public string Type { get; set; }
    }
    public class SyncSaleObject
    {
        public string Type { get; set; }
        public object Data { get; set; }
    }
}
