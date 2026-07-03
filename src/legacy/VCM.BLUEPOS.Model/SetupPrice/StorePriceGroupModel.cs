using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPrice
{
    public class StorePriceGroupModel
    {
        public string Store { get; set; }
        public string PriceGroupCode { get; set; }
        public int Type { get; set; }
        public int Priority { get; set; }
        public int ReplicationCounter { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Pkey { get; set; }
    }

    public class PriceGroupModel
    {
        public string PriceGroupCode { get; set; }
        public string PriceGroupName { get; set; }
        public int Priority { get; set; }
        public bool Enabled { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Pkey { get; set; }

        public Nullable<System.DateTime> CreatedDate { get; set; }
    }
}
