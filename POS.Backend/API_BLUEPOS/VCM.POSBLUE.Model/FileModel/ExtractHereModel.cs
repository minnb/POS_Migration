using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.FileModel
{
   public class ExtractHereModel
    {
        public string SiteCode { get; set; }
        public List<byte[]> ListFileByte { get; set; }
    }
}
