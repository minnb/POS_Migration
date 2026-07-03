using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace VCM.BLUEPOS.Models
{
    public class DataTablesViewModel<T> where T : class
    {
        public string draw { get; set; }
        public int recordsFiltered { get; set; }
        public int recordsTotal { get; set; }
        public IEnumerable<T> data { get; set; }
    }
}