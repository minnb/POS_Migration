using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Reward
{
    public class RewardCodeRequest
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public string OrderNo { get; set; }
        public DateTime BussinessDate { get; set; }
        public string OfferNo { get; set; }
        public string IPServer { get; set; }
    }
}
