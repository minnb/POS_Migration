using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.FileModel
{
   public class SyncFileLogModel
    {
        public int ID { get; set; }
        public string FileName { get; set; }
        public string TableName { get; set; }
        public Nullable<System.DateTime> Date { get; set; }
        public string Folder { get; set; }
        public Nullable<short> Status { get; set; }
        public string ErrorMsg { get; set; }
        public string SyncType { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
    }
}
