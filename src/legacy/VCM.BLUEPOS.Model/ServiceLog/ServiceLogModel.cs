using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.ServiceLog
{
    public class StackTraceLogModel
    {
        public long ID { get; set; }
        public string UserName { get; set; }
        public string StoreNo { get; set; }
        public string POSTerminal { get; set; }
        public string Description { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string ActionType { get; set; }
        public string JSONModel { get; set; }
        public string MessageError { get; set; }
        public string ErrorDateTime { get; set; }
        public string IPServer { get; set; }
        public string URL { get; set; }
        public string Source { get; set; }
        public string RefKey1 { get; set; }
        public string RefKey2 { get; set; }
        public string RefKey3 { get; set; }
        public string RefKey4 { get; set; }
        public string RefKey5 { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }

    }

}
