using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCM.BLUEPOS.Model.SetupPromotion
{
   public class BuyGroupItemModel
    {
        public int ID { get; set; }
        public string GroupCode { get; set; }
        public string GroupName { get; set; }
        public string ListItemNo { get; set; }
        public string CreatedUser { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public string LastUpdateUser { get; set; }
        public Nullable<System.DateTime> LastUpdateDate { get; set; }
        public Nullable<bool> Status { get; set; }
    }
}
