using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.POSBLUE.Data.WriteLog;
using VCM.POSBLUE.Model.FileModel;

namespace VCM.POSBLUE.Business.WriteLog
{
    public interface ISyncFileLogBLO
    {
        //bool WriteSyncFileLog(List<SyncFileLogModel> listFile);
        List<SyncFileLogModel> CreateModelListFileLog(SyncFileLogModel model);
    }
   public class SyncFileLogBLO: ISyncFileLogBLO
    {
        private SyncFileLogData data { get; set; }
        public SyncFileLogBLO()
        {
            data = new SyncFileLogData();
        }

        //public bool WriteSyncFileLog(List<SyncFileLogModel> listFile)
        //{
        //    return data.WriteSyncFileLog(listFile);
        //}

        public List<SyncFileLogModel> CreateModelListFileLog(SyncFileLogModel model)
        {
            return data.CreateModelListFileLog(model);
        }
    }
}
