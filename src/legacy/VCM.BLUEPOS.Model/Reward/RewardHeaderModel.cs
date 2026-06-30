using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Reward
{
    public class RewardHeaderModel
    {
        public string RewardNo { get; set; }
        public string Title { get; set; }
        public Nullable<System.DateTime> FromDate { get; set; }
        public Nullable<System.DateTime> ToDate { get; set; }
        public string OfferNo { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public Nullable<bool> Enabled { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Pkey { get; set; }
        public bool? IsReward { get; set; }
    }
}
