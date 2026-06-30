using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class BussinessDateConfirmModel
    {
        public string Code { get; set; }
        public string StoreNo { get; set; }
        public Nullable<System.DateTime> BussinessDate { get; set; }
        public Nullable<double> TotalAmount { get; set; }
        public Nullable<bool> IsConfirm { get; set; }
        public Nullable<System.DateTime> ConfirmDate { get; set; }
        public string FileNameDetail { get; set; }
        public string CreatedUser { get; set; }
        public string UpdatedUser { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
    }

}
