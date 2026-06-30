using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class BussinessDateOpenModel
    {
        public string Code { get; set; }
        public string StoreNo { get; set; }
        public Nullable<System.DateTime> BussinessDate { get; set; }
        public Nullable<System.Boolean> StatusConfirm { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public string FormatBussinessDate { get { return string.Format("{0:dd/MM/yyyy}", BussinessDate); } }
        public string FormatUpdatedUser { get { return UpdatedUser == null ? "" : string.Format("{0:dd/MM/yyyy HH:mm:ss}", UpdatedDate); } }
    }

}
