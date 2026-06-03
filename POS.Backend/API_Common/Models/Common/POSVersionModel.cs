using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.Common
{
    public class POSVersionModel
    {
        public string LastVersion { get; set; }
        public string CurVersion { get; set; }
        public Nullable<System.DateTime> UpdateTime { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Source { get; set; }
        public string Pkey { get; set; }
        public Nullable<bool> IsUpdate { get; set; }
        public string Folder { get; set; }
    }
}
