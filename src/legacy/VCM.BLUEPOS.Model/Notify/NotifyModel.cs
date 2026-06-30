using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Notify
{
    public class NotifyModel
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Nullable<DateTime> FromDate { get; set; }
        public Nullable<DateTime> ToDate { get; set; }
        public Nullable<bool> Status { get; set; }
        public Nullable<DateTime> CreatedDate { get; set; }
        public string CreatedUser { get; set; }
    }
    public class NotifySaveModel
    {
        public int ID { get; set; }
        public string Title { get; set; }

        public string Description { get; set; }
        public string FromDateStr { get; set; }
        public string ToDateStr { get; set; }
        public Nullable<DateTime> FromDate { get; set; }
        public Nullable<DateTime> ToDate { get; set; }
        public Nullable<bool> Status { get; set; }
        public Nullable<DateTime> CreatedDate { get; set; }
        public string CreatedUser { get; set; }
    }
    public class NotifyViewClientModel
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }       
    }
    public class NotifyConfirmModel
    {
        public Nullable<int> NotifyID { get; set; }
        public string Title { get; set; }
        public string Username { get; set; }
        public Nullable<DateTime> CreatedDate { get; set; }
        public Nullable<bool> Status { get; set; }
    }
}
