using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class ShiftLineArray
    {
        public int LineNo { get; set; }
        public string CashCode { get; set; }
        public Nullable<int> CashValue { get { return int.Parse(CashCode); } }
        public Nullable<int> CashQuantity { get; set; }
        public Nullable<double> CashAmount { get { return (double)CashQuantity * (double)CashValue; } }
    }

}
