using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Invoice
{
    public class BranchModel
    {
        public int ID { get; set; }
        public string BranchNo { get; set; }
        public string BranchName { get; set; }
        public string Address { get; set; }
        public string CompTaxCode { get; set; }      
        public string ETaxAccName { get; set; }
        public string ETaxAccPass { get; set; }
        public string ETaxWSName { get; set; }
        public string ETaxWSPass { get; set; }
        public Nullable<System.DateTime> CreateDate { get; set; }
        public Nullable<System.DateTime> LastUpdate { get; set; }
        public string URL { get; set; }
    }
}
