using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class ShiftHeaderModel
    {
        public string ShiftCode { get; set; }
        public string StaffCode { get; set; }
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public Nullable<System.DateTime> BussinessDate { get; set; }
        public string ShiftNumber { get; set; }
        public Nullable<double> BeginAmount { get; set; }
        public Nullable<double> CloseAmount { get; set; }
        public Nullable<double> OutAmount { get; set; }
        public Nullable<int> QuantityBox { get; set; }
        public Nullable<int> QuantityCoupon { get; set; }
        public Nullable<int> QuantityVoucher { get; set; }
        public Nullable<bool> IsShiftOpened { get; set; }
        public Nullable<bool> IsShiftClosed { get; set; }
        public Nullable<System.DateTime> OpenShiftDate { get; set; }
        public Nullable<System.DateTime> CloseShiftDate { get; set; }
        public string CreatedUser { get; set; }
        public string Source { get; set; }
        public string UpdatedUser { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string FormatBussinessDate { get { return string.Format("{0:dd/MM/yyyy}", BussinessDate); } }
        public string FormatOutAmount { get { return string.Format("{0:###,###}", OutAmount); } }
    }



}
