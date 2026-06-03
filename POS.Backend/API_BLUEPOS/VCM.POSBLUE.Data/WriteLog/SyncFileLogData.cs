using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCM.POSBLUE.Data.DataBaseContext;
using VCM.POSBLUE.Model.FileModel;

namespace VCM.POSBLUE.Data.WriteLog
{
    public class SyncFileLogData
    {
        public List<SyncFileLogModel> CreateModelListFileLog(SyncFileLogModel model)
        {
            var result = new List<SyncFileLogModel>
            {
                new SyncFileLogModel
                {
                    FileName = model.FileName,
                    TableName = model.TableName,
                    Date = model.Date,
                    Folder = model.Folder,
                    Status = model.Status,
                    ErrorMsg = model.ErrorMsg,
                    SyncType = model.SyncType,
                    CreatedDate = model.CreatedDate
                }
            };
            return result;
        }
    }
}
