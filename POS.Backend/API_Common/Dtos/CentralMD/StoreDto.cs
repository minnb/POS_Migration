using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TCX.API.Common.Dtos.CentralMD
{
    public class StoreDto
    {
        public string StoreNo { get; set; }
        public string No { get; set; }
        public string ResponsibilityCenter { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string PostCode { get; set; }
        public string TaxCode { get; set; }
        public string BusinessAreaNo { get; set; }
        public string ConnectionString { get; set; }
    }
}
