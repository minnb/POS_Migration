using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.LogsData
{
    public class SyncBussinessDateToCentralModel
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public string NgayKD { get; set; }
        public string IsClosed { get; set; }
        public string CloseDate { get; set; }
        public string CloseUser { get; set; }
    }

    public class SyncBussinessDateModel
    {
        public string StoreNo { get; set; }
        public string PosID { get; set; }
        public DateTime? NgayKD { get; set; }
        public bool? IsClosed { get; set; }
        public DateTime? CloseDate { get; set; }
        public string CloseUser { get; set; }
    }



}
