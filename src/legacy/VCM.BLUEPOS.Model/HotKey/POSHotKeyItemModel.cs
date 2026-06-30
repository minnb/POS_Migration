using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.HotKey
{
    public class POSHotKeyItemModel
    {
        public string StoreGroup { get; set; }
        public string ProvinceName { get; set; }
        public string Barcode { get; set; }
        public string ItemNo { get; set; }
        public string Description { get; set; }
        public Nullable<int> Seq { get; set; }
        public Nullable<long> Counter { get; set; }
        public string Pkey { get; set; }
        public bool? Status { get; set; }
    }

}
