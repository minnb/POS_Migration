using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.GotIt
{
    public class GotItVoucherCheckResponse
    {
        public bool valid { get; set; }
        public int state { get; set; }
        public string message { get; set; }
        public string message_vi { get; set; }
        public string usedDate { get; set; }
        public string expiryDate { get; set; }
        public string cancelDate { get; set; }
        public GotItProductCheckResponse products { get; set; }
        //public GotItStoreCheckResponse used_store { get; set; }
    }
    public class GotItProductCheckResponse
    {
        public string product_name_vi { get; set; }
        public string product_name_en { get; set; }
        public string type { get; set; }
        public string value { get; set; }
        public string poscode { get; set; }
        public string sku { get; set; }
    }
    public class GotItStoreCheckResponse
    {
        public string name_vi { get; set; }
        public string name_en { get; set; }

    }

}
