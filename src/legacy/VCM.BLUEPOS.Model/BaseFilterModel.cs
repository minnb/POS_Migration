using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model
{
    public class BaseFilterModel
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public bool IsPaging { get; set; }
        public string SearchText { get; set; } = string.Empty;
        public bool Status { get; set; }
        public int LastId { get; set; }
    }
}
