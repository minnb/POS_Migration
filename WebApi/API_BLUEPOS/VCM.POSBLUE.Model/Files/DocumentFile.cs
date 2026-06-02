using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.FileDocument.Model.Files
{
   public class DocumentFile
    {
        public string FileName { get; set; }
        public int FileSize { get; set; }
        public string Folder { get; set; }
    }
}
