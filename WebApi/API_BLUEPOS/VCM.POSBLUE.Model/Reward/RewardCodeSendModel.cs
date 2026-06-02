using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Reward
{
    public class RewardCodeSendModel
    {
        public string Code { get; set; }
        public string OfferNo { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsReward { get; set; }
        public string SubDesc { get; set; }
    }
}
