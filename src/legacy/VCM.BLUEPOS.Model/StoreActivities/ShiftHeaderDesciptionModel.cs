using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.StoreActivities
{
    public class ViewShiftHeaderDesciptionModel
    {
        public string ShiftCode { get; set; }
        public string StoreNo { get; set; }
        public DateTime? BussinessDate { get; set; }
        public string ViewTitle { get; set; }
        public ShiftHeaderDesciptionModel DescriptionModel { get; set; }
    }

    public class ShiftHeaderDesciptionModel
    {
        public long ID { get; set; }
        public string StoreNo { get; set; }
        public DateTime? BussinessDate { get; set; }
        public string ShiftCode { get; set; }
        public string Descriptions { get; set; }
        public string CreatedUser { get; set; }
        public DateTime? CreatedDate { get; set; }
    }


}
