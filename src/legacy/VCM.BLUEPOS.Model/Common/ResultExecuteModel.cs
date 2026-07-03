using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Common
{
    public class ResultExecuteModel
    {
        public int Status { get; set; }
        public string IPAddress { get; set; }
        public string PosTerminal { get; set; }
        public string Message { get; set; }
        public string HtmlData { get; set; }
        public List<ResultDataTable> GetDataTable { get; set; }
    }

    public class ResultDataTable
    {
        public string TableName { get; set; }
        public string[] DataColumn { get; set; }
        public object[][] DataRow { get; set; }
    }
}
