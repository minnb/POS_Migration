using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class ViewPopupNotEnd
    {
        public string StoreNo { get; set; }
        public string StaffCode { get; set; }
        public string PosTerminal { get; set; }
        public Nullable<bool> IsFinished { get; set; }
        public Nullable<DateTime> BussinessDate { get; set; }
        public string FormatBussinessDate { get { return string.Format("{0:dd/MM/yyyy}", BussinessDate); } }
        public int Status { get; set; }
        public string Message { get; set; }


    }


}
