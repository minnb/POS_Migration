using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class POSBussinessDateModel
    {
        public string Code { get; set; }
        public string StoreNo { get; set; }
        public string PosTerminal { get; set; }
        public Nullable<System.DateTime> BussinessDate { get; set; }
        public Nullable<double> BeginAmount { get; set; }
        public Nullable<double> CloseAmount { get; set; }
        public Nullable<bool> IsOpened { get; set; }
        public Nullable<bool> IsClosed { get; set; }
        public Nullable<System.DateTime> OpenDate { get; set; }
        public Nullable<System.DateTime> CloseDate { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string FormatBussinessDate { get { return string.Format("{0:dd/MM/yyyy}", BussinessDate); } }
        public string FormatBeginAmount { get { return string.Format("{0:###,###}", BeginAmount); } }
    }


}
