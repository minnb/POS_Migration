using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.LogsData
{
    public class LogFileSalePOSModel
    {
        public string OrderNo { get; set; }
        public string FileName { get; set; }
        public string TableName { get; set; }
        public Nullable<int> IsError { get; set; }
        public string MsgError { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> FromDate { get; set; }
        public Nullable<System.DateTime> ToDate { get; set; }
        public string IpServer { get; set; }
        public string ProcessID { get; set; }
    }



}
