using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.Home
{
    public class ViewCheckListDailyModel
    {
        public List<CheckListUserModel> ListUserCheck { get; set; }
        public List<CheckListUserModel> ListUserChecked { get; set; }
        public List<CheckListDailyModel> CheckList { get; set; }

    }
    public class CheckListDailyModel
    {
        public int ID { get; set; }
        public string CheckListName { get; set; }
        public string TimeCheck { get; set; }
        public string OwnerCheck { get; set; }
        public string Note { get; set; }
        public string UserCheck { get; set; }
        public DateTime? DateTimeCheck { get; set; }
        public bool? IsCheck { get; set; }
        //public List<CheckListUserModel> ListUserCheck { get; set; }
    }
    public class UserCheckModel
    {
        public string UserCheck { get; set; }
        public DateTime? DateTimeCheck { get; set; }
        public int CountCheck { get; set; }
    }

    public class CheckListDailyViewModel
    {
        public List<CheckListDailyModel> CheckListUser { get; set; }
        public List<UserCheckModel> ListCheck { get; set; }
    }

    public class ReportCheckListResponse
    {
        public DateTime DayCheck { get; set; }
        public string UserCheck { get; set; }
        public int CountCheck { get; set; }
        public int TotalCheck { get; set; }
        public int IsSuccess { get; set; }

    }
}
