using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.LogsData
{
    public class SyncChangeLogStoreModel
    {
        public decimal ID { get; set; }
        public string SiteCode { get; set; }
        public string TableName { get; set; }
        public long? LastCounter { get; set; }
        public string SyncTimeStr { get; set; }
        public string LastedDateStr { get; set; }
        public int Total { get; set; }
    }

    public class UpdateSyncChangeLogStoreModel
    {
        public decimal ID { get; set; }
        public string SiteCode { get; set; }
        public string TableName { get; set; }
        public long? LastCounter { get; set; }
        public string SyncTimeStr { get; set; }
        public string LastedDateStr { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

    }


}
