using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class LogMisSaleModel
    {
        public string StoreNo { get; set; }
        public Nullable<System.DateTime> BussinessDate { get; set; }
        public Nullable<System.DateTime> ToDate { get; set; }
        public Nullable<int> TotalSalePOS { get; set; }
        public Nullable<int> TotalSaleServer { get; set; }
        public string CreatedUser { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string FormatBussinessDate { get { return string.Format("{0:dd/MM/yyyy}", BussinessDate); } }
        public string FormatCreatedDate { get { return string.Format("{0:dd/MM/yyyy HH:mm}", CreatedDate); } }
    }

}
