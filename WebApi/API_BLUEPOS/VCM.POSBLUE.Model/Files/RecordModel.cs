using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.FileDocument.Model.Files
{
    public class RecordModel
    {
        public string Title { get; set; }
        public int? PositionTitle { get; set; }
        public string FileName { get; set; }
        public string Folder { get; set; }
        public decimal? SizeName { get; set; }
        public string FileUrl { get; set; }
    }
}
