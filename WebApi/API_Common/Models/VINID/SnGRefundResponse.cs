using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.VINID
{
   public class SnGRefundResponse
    {
        public SnGRefundMeta meta { get; set; }
        public SnGRefundData data { get; set; }
    }
    public class SnGRefundMeta
    {
        public int code { get; set; }
        public string message { get; set; }
    }
    public class SnGRefundData
    {
        public int vin_id_earn { get; set; }
    }

    public class SnGRefundDataPOS
    {
        public int VinIDEarn { get; set; }
        public int VinIDRedeem { get; set; }
    }
}
