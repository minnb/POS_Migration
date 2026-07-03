using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class ShiftLineModel
    {
        public string ShiftCode { get; set; }
        public int LineNo { get; set; }
        public string CashCode { get; set; }
        public Nullable<int> CashValue { get; set; }
        public Nullable<int> CashQuantity { get; set; }
        public Nullable<double> CashAmount { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
    }

}
