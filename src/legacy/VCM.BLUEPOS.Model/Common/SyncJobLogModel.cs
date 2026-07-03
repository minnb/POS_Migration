using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Common
{
    public class SyncJobLogModel
    {
        public string Type { get; set; }
        public string Message { get; set; }
        public int? TotalRecord { get; set; }
        public string TableName { get; set; }
        public string FileName { get; set; }
        public string SiteNo { get; set; }
    }

    public class SyncDataToAllPosModel
    {
        public string SysTem { get; set; }
        public string Chanel { get; set; }
        public int iPageNumber { get; set; }
        public int iPageSize { get; set; }
        public bool? IsFirst { get; set; }
        public bool? IsAll { get; set; }
        public string iStoreNo { get; set; }
        public bool? IsUpLoad { get; set; }
    }

}
