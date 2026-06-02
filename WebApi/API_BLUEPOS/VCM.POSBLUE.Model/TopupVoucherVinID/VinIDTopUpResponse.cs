using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.TopupVoucherVinID
{
    public class VinIDTopUpResponse
    {
        public TopUpMeta meta { get; set; }
        public TopUpData data { get; set; }
    }
    public class TopUpMeta
    {
        public int code { get; set; }
        public string message { get; set; }
    }
    public class TopUpData
    {
        public string full_name { get; set; }
        public string phone_number { get; set; }
        public string dob { get; set; }
        public string gender { get; set; }
        public string identify { get; set; }
        public string status { get; set; }// A: Active | I: Inactive | B: Block
        public string card_status { get; set; }// A: Active | I: Inactive

    }

    public class VinIDTopUpPointResponse
    {
        public TopUpMeta meta { get; set; }
    }

    public class VinIDTopUpStatusOrderResponse
    {
        public TopUpMeta meta { get; set; }
        public TopUpStatusOrderData data { get; set; }
    }
    public class TopUpStatusOrderData
    {
        public string status { get; set; }
        public string type { get; set; }
    }
}
