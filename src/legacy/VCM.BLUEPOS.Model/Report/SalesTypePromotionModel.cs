using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Report
{
    public class SalesTypePromotionModel
    {
        public string Code { get; set; }
        public string TenderType { get; set; }
        public string Description { get; set; }
        public Nullable<double> Percent { get; set; }
        public string ImageName { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<int> Order { get; set; }
        public string HotKey { get; set; }
        public Nullable<int> Counter { get; set; }
        public string Pkey { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }

}
