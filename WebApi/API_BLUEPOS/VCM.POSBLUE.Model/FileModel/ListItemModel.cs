using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.POSBLUE.Model.FileModel
{
   public class ListItemModel
    {
        public string FileName { get; set; }
        public string TableName { get; set; }
        public string Action { get; set; }
        public List<ItemModel> Data { get; set; }
    }
}
