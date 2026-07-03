using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Common
{
    public class DataTableModel
    {
        public string TABLE_CATALOG { get; set; }
        public string TABLE_SCHEMA { get; set; }
        public string TABLE_NAME { get; set; }
        public string TABLE_TYPE { get; set; }
    }
    public class ColumnNameModel
    {
        public string COLUMN_NAME { get; set; }
    }
    public class DataTableSvrModel
    {
        public string DatabaseName { get; set; }
    }
}
