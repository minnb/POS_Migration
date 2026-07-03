using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.HotKey
{
    public class ProvinceModel
    {
        public int ID { get; set; }
        public string ProvinceCode { get; set; }
        public string ProvinceName { get; set; }
        public Nullable<bool> Enable { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string CreatedUser { get; set; }
        public Nullable<System.DateTime> LastUpdateDate { get; set; }
        public string LastUpdateUser { get; set; }
        public Nullable<long> Counter { get; set; }
        public string PKey { get; set; }
    }

}
