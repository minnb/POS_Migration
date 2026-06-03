using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.FileModel
{
    public class FilePathModel
    {
        public string SiteCode { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FolderAPI { get; set; }
    }

    public class PathFileAPIModel
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string IPServer { get; set; }
        public string PathFileIPServer { get; set; }
        public string NetworkPathDisc { get; set; }
        public string FolderAPI { get; set; }

    }
    public class FileExistDelete
    {
        public string StoreNo { get; set; }
        //public List<PathFileAPIModel> ListFileDelete { get; set; }
    }

    public class ListFileNameModel
    {
        public string FileName { get; set; }
    }
}
