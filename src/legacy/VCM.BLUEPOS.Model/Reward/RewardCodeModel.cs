using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Reward
{
   public class RewardCodeModel
    {
        public int ID { get; set; }
        public string RewardNo { get; set; }
        public string Code { get; set; }
        public string OrderNo { get; set; }
        public string OfferNo { get; set; }
        public Nullable<bool> Enabled { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Pkey { get; set; }
    }
}
